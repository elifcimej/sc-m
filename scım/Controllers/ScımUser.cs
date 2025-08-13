
using System.Text.Json.Serialization;

namespace ScimIdentityManager.Models
{
    public class ScimUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public ScimName? Name { get; set; }

        [JsonPropertyName("emails")]
        public List<ScimEmail> Emails { get; set; } = new List<ScimEmail>();

        [JsonPropertyName("active")]
        public bool Active { get; set; } = true;

        [JsonPropertyName("groups")]
        public List<ScimGroup>? Groups { get; set; }

        [JsonPropertyName("meta")]
        public ScimMeta? Meta { get; set; }

        [JsonPropertyName("schemas")]
        public List<string> Schemas { get; set; } = new List<string> { "urn:ietf:params:scim:schemas:core:2.0:User" };
    }

    public class ScimName
    {
        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("formatted")]
        public string? Formatted { get; set; }
    }

    public class ScimEmail
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("primary")]
        public bool Primary { get; set; } = true;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "work";
    }

    public class ScimGroup
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("display")]
        public string? Display { get; set; }
    }

    public class ScimMeta
    {
        [JsonPropertyName("resourceType")]
        public string ResourceType { get; set; } = "User";

        [JsonPropertyName("created")]
        public DateTime? Created { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime? LastModified { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}
