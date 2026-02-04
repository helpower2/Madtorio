using Microsoft.AspNetCore.Identity;

namespace Madtorio.Services;

public interface IUserManagementService
{
    /// <summary>
    /// Retrieves all users in the system.
    /// </summary>
    Task<List<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Retrieves a specific user by ID.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    Task<string?> GetCurrentUserIdAsync();

    /// <summary>
    /// Creates a new user with a temporary password.
    /// Sets RequirePasswordChange to true so user must change password on first login.
    /// </summary>
    Task<IdentityResult> CreateUserAsync(string username, string temporaryPassword, string? email = null, bool isAdmin = false);

    /// <summary>
    /// Creates a new user for self-registration (no password change required).
    /// </summary>
    Task<IdentityResult> RegisterUserAsync(string username, string password, string email);

    /// <summary>
    /// Updates an existing user's username, email, and admin status.
    /// </summary>
    Task<IdentityResult> UpdateUserAsync(string userId, string username, string? email = null, bool isAdmin = false);

    /// <summary>
    /// Resets a user's password to a new temporary password.
    /// Sets RequirePasswordChange to true to force password change on next login.
    /// </summary>
    Task<IdentityResult> ResetPasswordAsync(string userId, string newTemporaryPassword);

    /// <summary>
    /// Deletes a user account.
    /// Prevents deletion of self or the last admin user.
    /// </summary>
    Task<IdentityResult> DeleteUserAsync(string userId);

    /// <summary>
    /// Toggles admin role for a user.
    /// Prevents removing admin role from self or the last admin.
    /// </summary>
    Task<IdentityResult> ToggleAdminRoleAsync(string userId, bool makeAdmin);

    /// <summary>
    /// Gets the count of admin users.
    /// </summary>
    Task<int> GetAdminCountAsync();
}

/// <summary>
/// Data transfer object for user information.
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public bool RequirePasswordChange { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public DateTime? CreatedDate { get; set; }
}
