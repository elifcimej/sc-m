using scım.Models;

namespace scım.Services
{
    public interface IScimService
    {
        Task<ScimUser> ConvertToScimUserAsync(User user);
        Task<User> ConvertFromScimUserAsync(ScimUser scimUser);
        Task<ScimGroup> ConvertToScimGroupAsync(Group group);
        Task<Group> ConvertFromScimGroupAsync(ScimGroup scimGroup);
        Task<bool> SyncUserToCloudServicesAsync(User user, string operation);
        Task<bool> SyncGroupToCloudServicesAsync(Group group, string operation);
    }
}
