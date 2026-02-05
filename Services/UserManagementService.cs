using Madtorio.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Madtorio.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                userDtos.Add(MapToDto(user, isAdmin));
            }

            return userDtos.OrderBy(u => u.UserName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            throw;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            return MapToDto(user, isAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            throw;
        }
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return _userManager.GetUserId(user);
        }
        return null;
    }

    public async Task<IdentityResult> CreateUserAsync(string username, string temporaryPassword, string? email = null, bool isAdmin = false)
    {
        try
        {
            // Check if username already exists
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null)
            {
                _logger.LogWarning("Attempted to create user with existing username: {Username}", username);
                return IdentityResult.Failed(new IdentityError { Description = "Username already exists." });
            }

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                RequirePasswordChange = true, // Force password change on first login
                PasswordLastChanged = null
            };

            // Create user with temporary password
            var createResult = await _userManager.CreateAsync(user, temporaryPassword);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Failed to create user {Username}: {Errors}", username, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return createResult;
            }

            // Assign admin role if requested
            if (isAdmin)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to assign admin role to user {Username}: {Errors}", username, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return roleResult;
                }
            }

            _logger.LogInformation("User {Username} created successfully", username);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", username);
            throw;
        }
    }

    public async Task<IdentityResult> RegisterUserAsync(string username, string password, string email)
    {
        try
        {
            // Check if username already exists
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null)
            {
                _logger.LogWarning("Attempted to register user with existing username: {Username}", username);
                return IdentityResult.Failed(new IdentityError { Description = "Username already exists." });
            }

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                RequirePasswordChange = false, // Self-registered users don't need to change password
                PasswordLastChanged = DateTime.UtcNow
            };

            // Create user with their chosen password
            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Failed to register user {Username}: {Errors}", username, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return createResult;
            }

            _logger.LogInformation("User {Username} registered successfully", username);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username}", username);
            throw;
        }
    }

    public async Task<IdentityResult> UpdateUserAsync(string userId, string username, string? email = null, bool isAdmin = false)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for update", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Check if new username is already taken by another user
            if (user.UserName != username)
            {
                var existingUser = await _userManager.FindByNameAsync(username);
                if (existingUser != null)
                {
                    _logger.LogWarning("Attempted to update user {UserId} to existing username: {Username}", userId, username);
                    return IdentityResult.Failed(new IdentityError { Description = "Username already exists." });
                }
            }

            user.UserName = username;
            user.Email = email;

            // Update user
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update user {UserId}: {Errors}", userId, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                return updateResult;
            }

            // Update admin role
            var currentlyAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin && !currentlyAdmin)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add admin role to user {UserId}: {Errors}", userId, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return roleResult;
                }
            }
            else if (!isAdmin && currentlyAdmin)
            {
                var roleResult = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove admin role from user {UserId}: {Errors}", userId, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return roleResult;
                }
            }

            _logger.LogInformation("User {UserId} updated successfully", userId);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            throw;
        }
    }

    public async Task<IdentityResult> ResetPasswordAsync(string userId, string newTemporaryPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for password reset", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Remove current password and set new one
            await _userManager.RemovePasswordAsync(user);
            var addPasswordResult = await _userManager.AddPasswordAsync(user, newTemporaryPassword);

            if (!addPasswordResult.Succeeded)
            {
                _logger.LogWarning("Failed to reset password for user {UserId}: {Errors}", userId, string.Join(", ", addPasswordResult.Errors.Select(e => e.Description)));
                return addPasswordResult;
            }

            // Set flag to require password change on next login
            user.RequirePasswordChange = true;
            user.PasswordLastChanged = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update password change flag for user {UserId}: {Errors}", userId, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                return updateResult;
            }

            _logger.LogInformation("Password reset for user {UserId}", userId);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        try
        {
            // Prevent deleting self
            var currentUserId = await GetCurrentUserIdAsync();
            if (userId == currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete self", userId);
                return IdentityResult.Failed(new IdentityError { Description = "You cannot delete your own account." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for deletion", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Prevent deleting last admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                var adminCount = await GetAdminCountAsync();
                if (adminCount <= 1)
                {
                    _logger.LogWarning("Attempted to delete last admin user {UserId}", userId);
                    return IdentityResult.Failed(new IdentityError { Description = "Cannot delete the last admin user." });
                }
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                _logger.LogWarning("Failed to delete user {UserId}: {Errors}", userId, string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
                return deleteResult;
            }

            _logger.LogInformation("User {UserId} deleted successfully", userId);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            throw;
        }
    }

    public async Task<IdentityResult> ToggleAdminRoleAsync(string userId, bool makeAdmin)
    {
        try
        {
            var currentUserId = await GetCurrentUserIdAsync();
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role toggle", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            var isCurrentlyAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            // Prevent removing admin role from self
            if (!makeAdmin && userId == currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to remove own admin role", userId);
                return IdentityResult.Failed(new IdentityError { Description = "You cannot remove your own admin role." });
            }

            // Prevent removing admin role from last admin
            if (!makeAdmin && isCurrentlyAdmin)
            {
                var adminCount = await GetAdminCountAsync();
                if (adminCount <= 1)
                {
                    _logger.LogWarning("Attempted to remove admin role from last admin {UserId}", userId);
                    return IdentityResult.Failed(new IdentityError { Description = "Cannot remove admin role from the last admin user." });
                }
            }

            if (makeAdmin && !isCurrentlyAdmin)
            {
                var addResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add admin role to user {UserId}: {Errors}", userId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    return addResult;
                }
                _logger.LogInformation("Admin role added to user {UserId}", userId);
            }
            else if (!makeAdmin && isCurrentlyAdmin)
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove admin role from user {UserId}: {Errors}", userId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return removeResult;
                }
                _logger.LogInformation("Admin role removed from user {UserId}", userId);
            }

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling admin role for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetAdminCountAsync()
    {
        try
        {
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole == null)
                return 0;

            var usersInAdminRole = await _userManager.GetUsersInRoleAsync("Admin");
            return usersInAdminRole.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin count");
            throw;
        }
    }

    private UserDto MapToDto(ApplicationUser user, bool isAdmin)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            IsAdmin = isAdmin,
            RequirePasswordChange = user.RequirePasswordChange,
            PasswordLastChanged = user.PasswordLastChanged
        };
    }
}
