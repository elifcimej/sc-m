using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using scım.Models;
using scım.Services;

namespace scım.Controllers
{
    [Route("scim/v2/Users")]
    [ApiController]
    public class ScimUserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IScimService _scimService;
        private readonly ILogger<ScimUserController> _logger;

        public ScimUserController(IUserService userService, IScimService scimService, ILogger<ScimUserController> logger)
        {
            _userService = userService;
            _scimService = scimService;
            _logger = logger;
        }

        // GET /scim/v2/Users
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int? startIndex = 1, [FromQuery] int? count = 100, [FromQuery] string? filter = null)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var scimUsers = new List<ScimUser>();

                foreach (var user in users)
                {
                    var scimUser = await _scimService.ConvertToScimUserAsync(user);
                    scimUsers.Add(scimUser);
                }

                var response = new ScimListResponse<ScimUser>
                {
                    TotalResults = scimUsers.Count,
                    StartIndex = startIndex ?? 1,
                    ItemsPerPage = count ?? 100,
                    Resources = scimUsers
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new ScimError
                {
                    Status = "500",
                    Detail = "Internal server error"
                });
            }
        }

        // GET /scim/v2/Users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var user = await _userService.GetUserByScimIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ScimError
                    {
                        Status = "404",
                        Detail = "User not found"
                    });
                }

                var scimUser = await _scimService.ConvertToScimUserAsync(user);
                return Ok(scimUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {Id}", id);
                return StatusCode(500, new ScimError
                {
                    Status = "500",
                    Detail = "Internal server error"
                });
            }
        }

        // POST /scim/v2/Users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] ScimUser scimUser)
        {
            try
            {
                if (scimUser == null)
                {
                    return BadRequest(new ScimError
                    {
                        Status = "400",
                        Detail = "Invalid request body"
                    });
                }

                // Check if user already exists
                if (!string.IsNullOrEmpty(scimUser.UserName) && await _userService.UserExistsByUserNameAsync(scimUser.UserName))
                {
                    return Conflict(new ScimError
                    {
                        Status = "409",
                        Detail = "User already exists"
                    });
                }

                if (!string.IsNullOrEmpty(scimUser.Emails?.FirstOrDefault()?.Value) && 
                    await _userService.UserExistsByEmailAsync(scimUser.Emails.First().Value))
                {
                    return Conflict(new ScimError
                    {
                        Status = "409",
                        Detail = "Email already exists"
                    });
                }

                var user = await _scimService.ConvertFromScimUserAsync(scimUser);
                var createdUser = await _userService.CreateUserAsync(user);

                // Sync to cloud services
                await _scimService.SyncUserToCloudServicesAsync(createdUser, "create");

                var responseUser = await _scimService.ConvertToScimUserAsync(createdUser);
                return CreatedAtAction(nameof(GetUser), new { id = responseUser.Id }, responseUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new ScimError
                {
                    Status = "500",
                    Detail = "Internal server error"
                });
            }
        }

        // PUT /scim/v2/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] ScimUser scimUser)
        {
            try
            {
                if (scimUser == null)
                {
                    return BadRequest(new ScimError
                    {
                        Status = "400",
                        Detail = "Invalid request body"
                    });
                }

                var existingUser = await _userService.GetUserByScimIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound(new ScimError
                    {
                        Status = "404",
                        Detail = "User not found"
                    });
                }

                var updatedUser = await _scimService.ConvertFromScimUserAsync(scimUser);
                updatedUser.Id = existingUser.Id;
                updatedUser.ScimId = id;
                updatedUser.CreatedAt = existingUser.CreatedAt;

                var result = await _userService.UpdateUserAsync(updatedUser);

                // Sync to cloud services
                await _scimService.SyncUserToCloudServicesAsync(result, "update");

                var responseUser = await _scimService.ConvertToScimUserAsync(result);
                return Ok(responseUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id}", id);
                return StatusCode(500, new ScimError
                {
                    Status = "500",
                    Detail = "Internal server error"
                });
            }
        }

        // DELETE /scim/v2/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userService.GetUserByScimIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ScimError
                    {
                        Status = "404",
                        Detail = "User not found"
                    });
                }

                var success = await _userService.DeleteUserAsync(user.Id);
                if (!success)
                {
                    return StatusCode(500, new ScimError
                    {
                        Status = "500",
                        Detail = "Failed to delete user"
                    });
                }

                // Sync to cloud services
                await _scimService.SyncUserToCloudServicesAsync(user, "delete");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, new ScimError
                {
                    Status = "500",
                    Detail = "Internal server error"
                });
            }
        }
    }
}
