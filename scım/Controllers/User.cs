
using System.ComponentModel.DataAnnotations;

namespace ScimIdentityManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string ExternalId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? DisplayName { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public string? Department { get; set; }

        public string? Title { get; set; }

        // SCIM specific fields
        public string? ScimId { get; set; }

        public List<string> Groups { get; set; } = new List<string>();

        // Cloud app sync status
        public List<CloudAppSync> CloudAppSyncs { get; set; } = new List<CloudAppSync>();
    }

    public class CloudAppSync
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string CloudAppName { get; set; } = string.Empty;
        public string? ExternalUserId { get; set; }
        public bool SyncStatus { get; set; }
        public DateTime LastSyncDate { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
