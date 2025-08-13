using Newtonsoft.Json;

namespace scım.Models
{
    // SCIM Resource base class
    public abstract class ScimResource
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        
        [JsonProperty("externalId")]
        public string? ExternalId { get; set; }
        
        [JsonProperty("meta")]
        public ScimMeta? Meta { get; set; }
        
        [JsonProperty("schemas")]
        public List<string> Schemas { get; set; } = new List<string>();
    }
    
    // SCIM Meta information
    public class ScimMeta
    {
        [JsonProperty("resourceType")]
        public string? ResourceType { get; set; }
        
        [JsonProperty("created")]
        public DateTime? Created { get; set; }
        
        [JsonProperty("lastModified")]
        public DateTime? LastModified { get; set; }
        
        [JsonProperty("location")]
        public string? Location { get; set; }
        
        [JsonProperty("version")]
        public string? Version { get; set; }
    }
    
    // SCIM User
    public class ScimUser : ScimResource
    {
        public ScimUser()
        {
            Schemas.Add("urn:ietf:params:scim:schemas:core:2.0:User");
        }
        
        [JsonProperty("userName")]
        public string? UserName { get; set; }
        
        [JsonProperty("name")]
        public ScimName? Name { get; set; }
        
        [JsonProperty("emails")]
        public List<ScimEmail>? Emails { get; set; }
        
        [JsonProperty("phoneNumbers")]
        public List<ScimPhoneNumber>? PhoneNumbers { get; set; }
        
        [JsonProperty("active")]
        public bool Active { get; set; } = true;
        
        [JsonProperty("title")]
        public string? Title { get; set; }
        
        [JsonProperty("department")]
        public string? Department { get; set; }
    }
    
    public class ScimName
    {
        [JsonProperty("formatted")]
        public string? Formatted { get; set; }
        
        [JsonProperty("familyName")]
        public string? FamilyName { get; set; }
        
        [JsonProperty("givenName")]
        public string? GivenName { get; set; }
    }
    
    public class ScimEmail
    {
        [JsonProperty("value")]
        public string? Value { get; set; }
        
        [JsonProperty("type")]
        public string? Type { get; set; } = "work";
        
        [JsonProperty("primary")]
        public bool Primary { get; set; } = true;
    }
    
    public class ScimPhoneNumber
    {
        [JsonProperty("value")]
        public string? Value { get; set; }
        
        [JsonProperty("type")]
        public string? Type { get; set; } = "work";
    }
    
    // SCIM Group
    public class ScimGroup : ScimResource
    {
        public ScimGroup()
        {
            Schemas.Add("urn:ietf:params:scim:schemas:core:2.0:Group");
        }
        
        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }
        
        [JsonProperty("members")]
        public List<ScimMember>? Members { get; set; }
    }
    
    public class ScimMember
    {
        [JsonProperty("value")]
        public string? Value { get; set; }
        
        [JsonProperty("$ref")]
        public string? Ref { get; set; }
        
        [JsonProperty("display")]
        public string? Display { get; set; }
    }
    
    // SCIM List Response
    public class ScimListResponse<T> where T : ScimResource
    {
        [JsonProperty("schemas")]
        public List<string> Schemas { get; set; } = new List<string> { "urn:ietf:params:scim:api:messages:2.0:ListResponse" };
        
        [JsonProperty("totalResults")]
        public int TotalResults { get; set; }
        
        [JsonProperty("startIndex")]
        public int StartIndex { get; set; }
        
        [JsonProperty("itemsPerPage")]
        public int ItemsPerPage { get; set; }
        
        [JsonProperty("Resources")]
        public List<T> Resources { get; set; } = new List<T>();
    }
    
    // SCIM Error Response
    public class ScimError
    {
        [JsonProperty("schemas")]
        public List<string> Schemas { get; set; } = new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" };
        
        [JsonProperty("status")]
        public string? Status { get; set; }
        
        [JsonProperty("scimType")]
        public string? ScimType { get; set; }
        
        [JsonProperty("detail")]
        public string? Detail { get; set; }
    }
}
