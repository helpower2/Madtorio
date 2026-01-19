using Microsoft.AspNetCore.Identity;

namespace Madtorio.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Indicates if the user is required to change their password on next login.
    /// Used for new users with temporary passwords and password resets.
    /// </summary>
    public bool RequirePasswordChange { get; set; } = false;

    /// <summary>
    /// Timestamp of when the password was last changed.
    /// Helps track password management history.
    /// </summary>
    public DateTime? PasswordLastChanged { get; set; }
}

