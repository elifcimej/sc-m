using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scım.Models
{
    [Table("Groups")]
    public class Group
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string? ExternalId { get; set; }
        
        // SCIM specific properties
        public string? ScimId { get; set; }
        
        public string? MetaLocation { get; set; }
        
        public string? MetaResourceType { get; set; } = "Group";
        
        public DateTime? MetaLastModified { get; set; }
        
        public string? MetaVersion { get; set; }
        
        // Navigation property for group members
        public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
    
    [Table("UserGroups")]
    public class UserGroup
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        public int GroupId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        
        public virtual Group Group { get; set; } = null!;
    }
}
