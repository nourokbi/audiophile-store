namespace Audiophile.Domain.Common;

// The one place input invariants are spelled out, so every entity rejects bad
// data the same way and with the same message shape. Each method returns the
// cleaned value, so it reads as an assignment rather than an if-block:
// Name = Guard.Text(name, 3, 50, nameof(name));
internal static class Guard
{
    public static string Text(string? value, int minLength, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);

        var trimmed = value.Trim();
        if (trimmed.Length < minLength || trimmed.Length > maxLength)
            throw new ArgumentException(
                $"{paramName} must be between {minLength} and {maxLength} characters.", paramName);

        return trimmed;
    }

    // Blank becomes null — an absent optional value has one representation, not two.
    public static string? OptionalText(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{paramName} must be at most {maxLength} characters.", paramName);

        return trimmed;
    }

    public static string Email(string? value, string paramName)
    {
        var trimmed = Text(value, 3, 256, paramName);
        var at = trimmed.IndexOf('@');

        // Deliberately loose. The only address that is provably deliverable is one
        // you have sent mail to; anything stricter here rejects valid addresses.
        if (at <= 0 || at != trimmed.LastIndexOf('@') || at == trimmed.Length - 1 || trimmed.Any(char.IsWhiteSpace))
            throw new ArgumentException($"{paramName} is not a valid email address.", paramName);

        return trimmed;
    }

    public static string Phone(string? value, string paramName)
    {
        var trimmed = Text(value, 7, 20, paramName);

        if (!trimmed.All(c => char.IsDigit(c) || c is '+' or '-' or ' ' or '(' or ')')
            || !trimmed.Any(char.IsDigit))
            throw new ArgumentException($"{paramName} is not a valid phone number.", paramName);

        return trimmed;
    }

    public static string? OptionalUrl(string? value, string paramName)
    {
        var trimmed = OptionalText(value, 2048, paramName);
        if (trimmed is null)
            return null;

        if (!Uri.TryCreate(trimmed, UriKind.RelativeOrAbsolute, out _))
            throw new ArgumentException($"{paramName} is not a valid URL.", paramName);

        return trimmed;
    }

    public static Guid NotEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{paramName} is required.", paramName);

        return value;
    }

    public static int Between(int value, int minimum, int maximum, string paramName)
    {
        if (value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(
                paramName, value, $"{paramName} must be between {minimum} and {maximum}.");

        return value;
    }

    public static int AtLeast(int value, int minimum, string paramName)
    {
        if (value < minimum)
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be at least {minimum}.");

        return value;
    }

    // Zero or more, and expressible in the decimal(18,2) the money columns use.
    public static decimal Money(decimal value, string paramName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} cannot be negative.");

        if (decimal.Round(value, 2) != value)
            throw new ArgumentException($"{paramName} cannot have more than two decimal places.", paramName);

        return value;
    }

    public static decimal PositiveMoney(decimal value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be greater than zero.");

        return Money(value, paramName);
    }

    public static TEnum Defined<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(paramName, value, $"{value} is not a valid {typeof(TEnum).Name}.");

        return value;
    }

    // Npgsql refuses to write a non-UTC DateTime to a timestamptz,
    // so every stored instant is normalised on the way in rather than at the boundary.
    public static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
