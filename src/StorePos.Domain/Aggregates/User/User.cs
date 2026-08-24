using StorePos.Domain.Base;
using StorePos.Domain.Enums;

namespace StorePos.Domain.Aggregates.User;

public sealed class User : AuditableEntity<long>, IAggregateRoot
{
    private User()
    {
    }

    private User(
        string username,
        string displayName,
        string? passwordHash,
        UserRole role)
    {
        Username = username;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public string Username { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public static User Create(
        string username,
        string displayName,
        string? passwordHash,
        UserRole role)
        => new(username, displayName, passwordHash, role);
}
