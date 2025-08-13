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
                                // Add Azure AD authentication
                                break;
                            case "Okta":
                                request.Headers.Add("Authorization", $"SSWS {integration["ApiToken"]}");
                                break;
                            case "GoogleWorkspace":
                                // Add Google Workspace authentication
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

                        // Add authentication headers
                        var integrationName = integration.Key;
                        switch (integrationName)
                        {
                            case "Okta":
                                request.Headers.Add("Authorization", $"SSWS {integration["ApiToken"]}");
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
    }
}
