using System.Security.Cryptography;

namespace UserService.Services;

/// <summary>
/// Hashes and verifies user passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a self-describing hash that embeds its own salt and iteration count.</summary>
    string Hash(string password);

    /// <summary>Verifies a candidate password against a stored hash.</summary>
    bool Verify(string password, string storedHash);
}

/// <summary>
/// PBKDF2-HMAC-SHA256 password hashing. The iteration count is stored alongside each hash so it
/// can be raised over time without invalidating existing credentials.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;

    // OWASP guidance for PBKDF2-HMAC-SHA256.
    private const int DefaultIterations = 600_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, DefaultIterations, HashAlgorithmName.SHA256, KeySize);

        return $"{DefaultIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedKey;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

        // Fixed-time comparison keeps the check free of timing side channels.
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
