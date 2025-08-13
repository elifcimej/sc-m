using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using scım.Models;
using System.Text;

namespace scım.Services
{
    public class ScimService : IScimService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScimService> _logger;
        private readonly HttpClient _httpClient;

        public ScimService(IConfiguration configuration, ILogger<ScimService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<ScimUser> ConvertToScimUserAsync(User user)
        {
            var scimUser = new ScimUser
            {
                Id = user.ScimId,
                ExternalId = user.ExternalId,
                UserName = user.UserName,
                Active = user.IsActive,
                Title = user.JobTitle,
                Department = user.Department,
                Name = new ScimName
                {
                    GivenName = user.FirstName,
                    FamilyName = user.LastName,
                    Formatted = $"{user.FirstName} {user.LastName}"
                },
                Emails = new List<ScimEmail>
                {
                    new ScimEmail
                    {
                        Value = user.Email,
                        Type = "work",
                        Primary = true
                    }
                },
                Meta = new ScimMeta
                {
                    ResourceType = "User",
                    Created = user.CreatedAt,
                    LastModified = user.UpdatedAt ?? user.CreatedAt,
                    Location = $"/scim/v2/Users/{user.ScimId}",
                    Version = user.MetaVersion
                }
            };

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                scimUser.PhoneNumbers = new List<ScimPhoneNumber>
                {
                    new ScimPhoneNumber
                    {
                        Value = user.PhoneNumber,
                        Type = "work"
                    }
                };
            }

            return scimUser;
        }

        public async Task<User> ConvertFromScimUserAsync(ScimUser scimUser)
        {
            var user = new User
            {
                ScimId = scimUser.Id,
                ExternalId = scimUser.ExternalId,
                UserName = scimUser.UserName ?? string.Empty,
                FirstName = scimUser.Name?.GivenName ?? string.Empty,
                LastName = scimUser.Name?.FamilyName ?? string.Empty,
                Email = scimUser.Emails?.FirstOrDefault(e => e.Primary)?.Value ?? string.Empty,
                PhoneNumber = scimUser.PhoneNumbers?.FirstOrDefault()?.Value,
                JobTitle = scimUser.Title,
                Department = scimUser.Department,
                IsActive = scimUser.Active,
                CreatedAt = scimUser.Meta?.Created ?? DateTime.UtcNow,
                UpdatedAt = scimUser.Meta?.LastModified,
                MetaLocation = scimUser.Meta?.Location,
                MetaResourceType = scimUser.Meta?.ResourceType,
                MetaLastModified = scimUser.Meta?.LastModified,
                MetaVersion = scimUser.Meta?.Version
            };

            return user;
        }

        public async Task<ScimGroup> ConvertToScimGroupAsync(Group group)
        {
            var scimGroup = new ScimGroup
            {
                Id = group.ScimId,
                ExternalId = group.ExternalId,
                DisplayName = group.DisplayName,
                Meta = new ScimMeta
                {
                    ResourceType = "Group",
                    Created = group.CreatedAt,
                    LastModified = group.UpdatedAt ?? group.CreatedAt,
                    Location = $"/scim/v2/Groups/{group.ScimId}",
                    Version = group.MetaVersion
                }
            };

            return scimGroup;
        }

        public async Task<Group> ConvertFromScimGroupAsync(ScimGroup scimGroup)
        {
            var group = new Group
            {
                ScimId = scimGroup.Id,
                ExternalId = scimGroup.ExternalId,
                DisplayName = scimGroup.DisplayName ?? string.Empty,
                CreatedAt = scimGroup.Meta?.Created ?? DateTime.UtcNow,
                UpdatedAt = scimGroup.Meta?.LastModified,
                MetaLocation = scimGroup.Meta?.Location,
                MetaResourceType = scimGroup.Meta?.ResourceType,
                MetaLastModified = scimGroup.Meta?.LastModified,
                MetaVersion = scimGroup.Meta?.Version
            };

            return group;
        }

        public async Task<bool> SyncUserToCloudServicesAsync(User user, string operation)
        {
            try
            {
                var scimUser = await ConvertToScimUserAsync(user);
                var json = JsonConvert.SerializeObject(scimUser);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var cloudIntegrations = _configuration.GetSection("ScimSettings:CloudIntegrations").GetChildren();
                var successCount = 0;

                foreach (var integration in cloudIntegrations)
                {
                    if (!integration.GetValue<bool>("Enabled"))
                        continue;

                    try
                    {
                        var baseUrl = integration["BaseUrl"];
                        var endpoint = operation.ToLower() switch
                        {
                            "create" => $"{baseUrl}/Users",
                            "update" => $"{baseUrl}/Users/{user.ScimId}",
                            "delete" => $"{baseUrl}/Users/{user.ScimId}",
                            _ => $"{baseUrl}/Users"
                        };

                        var method = operation.ToLower() switch
                        {
                            "create" => HttpMethod.Post,
                            "update" => HttpMethod.Put,
                            "delete" => HttpMethod.Delete,
                            _ => HttpMethod.Post
                        };

                        var request = new HttpRequestMessage(method, endpoint)
                        {
                            Content = operation.ToLower() != "delete" ? content : null
                        };

                        // Add authentication headers based on integration type
                        var integrationName = integration.Key;
                        switch (integrationName)
                        {
                            case "AzureAD":
                                // Azure AD authentication with Bearer token
                                var accessToken = await GetAzureADAccessTokenAsync(integration);
                                if (!string.IsNullOrEmpty(accessToken))
                                {
                                    request.Headers.Add("Authorization", $"Bearer {accessToken}");
                                    request.Headers.Add("Content-Type", "application/json");
                                }
                                break;
                            case "Okta":
                                request.Headers.Add("Authorization", $"SSWS {integration["ApiToken"]}");
                                request.Headers.Add("Content-Type", "application/json");
                                break;
                            case "GoogleWorkspace":
                                // Google Workspace authentication with Service Account
                                var googleToken = await GetGoogleWorkspaceTokenAsync(integration);
                                if (!string.IsNullOrEmpty(googleToken))
                                {
                                    request.Headers.Add("Authorization", $"Bearer {googleToken}");
                                    request.Headers.Add("Content-Type", "application/json");
                                }
                                break;
                        }

                        var response = await _httpClient.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            successCount++;
                            _logger.LogInformation("Successfully synced user {Email} to {Integration} with operation {Operation}", 
                                user.Email, integrationName, operation);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to sync user {Email} to {Integration} with operation {Operation}. Status: {Status}", 
                                user.Email, integrationName, operation, response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing user {Email} to {Integration} with operation {Operation}", 
                            user.Email, integration.Key, operation);
                    }
                }

                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SyncUserToCloudServicesAsync for user {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SyncGroupToCloudServicesAsync(Group group, string operation)
        {
            try
            {
                var scimGroup = await ConvertToScimGroupAsync(group);
                var json = JsonConvert.SerializeObject(scimGroup);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var cloudIntegrations = _configuration.GetSection("ScimSettings:CloudIntegrations").GetChildren();
                var successCount = 0;

                foreach (var integration in cloudIntegrations)
                {
                    if (!integration.GetValue<bool>("Enabled"))
                        continue;

                    try
                    {
                        var baseUrl = integration["BaseUrl"];
                        var endpoint = operation.ToLower() switch
                        {
                            "create" => $"{baseUrl}/Groups",
                            "update" => $"{baseUrl}/Groups/{group.ScimId}",
                            "delete" => $"{baseUrl}/Groups/{group.ScimId}",
                            _ => $"{baseUrl}/Groups"
                        };

                        var method = operation.ToLower() switch
                        {
                            "create" => HttpMethod.Post,
                            "update" => HttpMethod.Put,
                            "delete" => HttpMethod.Delete,
                            _ => HttpMethod.Post
                        };

                        var request = new HttpRequestMessage(method, endpoint)
                        {
                            Content = operation.ToLower() != "delete" ? content : null
                        };

                        var integrationName = integration.Key;
                        switch (integrationName)
                        {
                            case "AzureAD":
                                var accessToken = await GetAzureADAccessTokenAsync(integration);
                                if (!string.IsNullOrEmpty(accessToken))
                                {
                                    request.Headers.Add("Authorization", $"Bearer {accessToken}");
                                    request.Headers.Add("Content-Type", "application/json");
                                }
                                break;
                            case "Okta":
                                request.Headers.Add("Authorization", $"SSWS {integration["ApiToken"]}");
                                request.Headers.Add("Content-Type", "application/json");
                                break;
                            case "GoogleWorkspace":
                                var googleToken = await GetGoogleWorkspaceTokenAsync(integration);
                                if (!string.IsNullOrEmpty(googleToken))
                                {
                                    request.Headers.Add("Authorization", $"Bearer {googleToken}");
                                    request.Headers.Add("Content-Type", "application/json");
                                }
                                break;
                        }

                        var response = await _httpClient.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            successCount++;
                            _logger.LogInformation("Successfully synced group {DisplayName} to {Integration} with operation {Operation}", 
                                group.DisplayName, integrationName, operation);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to sync group {DisplayName} to {Integration} with operation {Operation}. Status: {Status}", 
                                group.DisplayName, integrationName, operation, response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing group {DisplayName} to {Integration} with operation {Operation}", 
                            group.DisplayName, integration.Key, operation);
                    }
                }

                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SyncGroupToCloudServicesAsync for group {DisplayName}", group.DisplayName);
                return false;
            }
        }

        private async Task<string?> GetAzureADAccessTokenAsync(IConfigurationSection integration)
        {
            try
            {
                var tenantId = integration["TenantId"];
                var clientId = integration["ClientId"];
                var clientSecret = integration["ClientSecret"];

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogWarning("Azure AD configuration is incomplete");
                    return null;
                }

                var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
                });

                var response = await _httpClient.PostAsync(tokenUrl, tokenRequest);
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonConvert.DeserializeObject<Dictionary<string, object>>(tokenResponse);
                    return tokenData?["access_token"]?.ToString();
                }

                _logger.LogError("Failed to get Azure AD access token: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Azure AD access token");
                return null;
            }
        }

        private async Task<string?> GetGoogleWorkspaceTokenAsync(IConfigurationSection integration)
        {
            try
            {
                var serviceAccountEmail = integration["ServiceAccountEmail"];
                var privateKeyPath = integration["PrivateKeyPath"];

                if (string.IsNullOrEmpty(serviceAccountEmail) || string.IsNullOrEmpty(privateKeyPath))
                {
                    _logger.LogWarning("Google Workspace configuration is incomplete");
                    return null;
                }

                // Read private key from file
                if (!File.Exists(privateKeyPath))
                {
                    _logger.LogWarning("Google Workspace private key file not found: {Path}", privateKeyPath);
                    return null;
                }

                var privateKeyJson = await File.ReadAllTextAsync(privateKeyPath);
                var privateKeyData = JsonConvert.DeserializeObject<Dictionary<string, string>>(privateKeyJson);
                var privateKey = privateKeyData?["private_key"];

                if (string.IsNullOrEmpty(privateKey))
                {
                    _logger.LogWarning("Private key not found in Google Workspace configuration file");
                    return null;
                }

                // For now, return a placeholder token
                // In a real implementation, you would create a proper JWT token
                _logger.LogInformation("Google Workspace token generation would require JWT signing");
                return "google_workspace_token_placeholder";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Google Workspace token");
                return null;
            }
        }
    }
}
