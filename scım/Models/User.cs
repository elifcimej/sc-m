using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scım.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string? Department { get; set; }
        
        [StringLength(100)]
        public string? JobTitle { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string? ExternalId { get; set; }
        
        // SCIM specific properties
        public string? ScimId { get; set; }
        
        public string? MetaLocation { get; set; }
        
        public string? MetaResourceType { get; set; } = "User";
        
        public DateTime? MetaLastModified { get; set; }
        
        public string? MetaVersion { get; set; }
    }
}
