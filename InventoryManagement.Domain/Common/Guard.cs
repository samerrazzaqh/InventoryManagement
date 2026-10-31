using InventoryManagement.Domain.Exceptions;

namespace InventoryManagement.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{name} must not exceed {maxLength} characters.");

        return trimmed;
    }

    public static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : NotBlank(value, name, maxLength);

    public static int Positive(int value, string name)
        => value > 0 ? value : throw new DomainException($"{name} must be greater than zero.");

    public static int NotNegative(int value, string name)
        => value >= 0 ? value : throw new DomainException($"{name} must not be negative.");

    public static decimal NotNegative(decimal value, string name)
        => value >= 0 ? value : throw new DomainException($"{name} must not be negative.");

    public static Guid NotEmpty(Guid value, string name)
        => value != Guid.Empty ? value : throw new DomainException($"{name} is required.");
}
