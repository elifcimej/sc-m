using Microsoft.AspNetCore.Mvc;
using scım.Models;
using scım.Services;

namespace scım.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IScimService _scimService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IScimService scimService, ILogger<UserController> logger)
        {
            _userService = userService;
            _scimService = scimService;
            _logger = logger;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserName,FirstName,LastName,Email,PhoneNumber,Department,JobTitle")] User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if user already exists
                    if (await _userService.UserExistsByEmailAsync(user.Email))
                    {
                        ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                        return View(user);
                    }

                    if (await _userService.UserExistsByUserNameAsync(user.UserName))
                    {
                        ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten kullanılıyor.");
                        return View(user);
                    }

                    var createdUser = await _userService.CreateUserAsync(user);

                    // Sync to cloud services
                    await _scimService.SyncUserToCloudServicesAsync(createdUser, "create");

                    TempData["SuccessMessage"] = "Kullanıcı başarıyla oluşturuldu ve bulut servislerine senkronize edildi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError("", "Kullanıcı oluşturulurken bir hata oluştu.");
                }
            }
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserName,FirstName,LastName,Email,PhoneNumber,Department,JobTitle,IsActive")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _userService.GetUserByIdAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Preserve SCIM-specific fields
                    user.ScimId = existingUser.ScimId;
                    user.ExternalId = existingUser.ExternalId;
                    user.CreatedAt = existingUser.CreatedAt;
                    user.MetaLocation = existingUser.MetaLocation;
                    user.MetaResourceType = existingUser.MetaResourceType;
                    user.MetaLastModified = existingUser.MetaLastModified;
                    user.MetaVersion = existingUser.MetaVersion;

                    var updatedUser = await _userService.UpdateUserAsync(user);

                    // Sync to cloud services
                    await _scimService.SyncUserToCloudServicesAsync(updatedUser, "update");

                    TempData["SuccessMessage"] = "Kullanıcı başarıyla güncellendi ve bulut servislerine senkronize edildi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user {Id}", id);
                    ModelState.AddModelError("", "Kullanıcı güncellenirken bir hata oluştu.");
                }
            }
            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var success = await _userService.DeleteUserAsync(id);
                if (success)
                {
                    // Sync to cloud services
                    await _scimService.SyncUserToCloudServicesAsync(user, "delete");

                    TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi ve bulut servislerinden kaldırıldı.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
