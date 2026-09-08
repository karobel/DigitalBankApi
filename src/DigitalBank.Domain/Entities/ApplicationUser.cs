namespace DigitalBank.Domain.Entities;

/// <summary>
/// Application user account used for authentication (bank employee / API consumer).
/// Kept simple and separate from ASP.NET Core Identity's heavier IdentityUser
/// so the Domain layer has zero framework dependencies.
/// </summary>
public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee"; // "Admin" | "Employee"
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
