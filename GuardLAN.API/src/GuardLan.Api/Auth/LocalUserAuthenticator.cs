using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace GuardLan.Api.Auth;

public sealed class LocalUserAuthenticator(
    IOptions<GuardLanAuthOptions> options,
    IHostEnvironment environment)
{
    public bool ValidateCredentials(string? username, string? password)
    {
        var configured = options.Value;
        var expectedUsername = Normalize(configured.AdminUsername);
        var submittedUsername = Normalize(username);

        if (expectedUsername.Length == 0 ||
            submittedUsername.Length == 0 ||
            !SecureEquals(submittedUsername, expectedUsername) ||
            string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(configured.AdminPasswordHash))
        {
            return VerifyPasswordHash(password, configured.AdminPasswordHash);
        }

        if (!string.IsNullOrEmpty(configured.AdminPassword))
        {
            return SecureEquals(password, configured.AdminPassword);
        }

        return environment.IsDevelopment() &&
               SecureEquals(expectedUsername, "guardlan") &&
               SecureEquals(password, GuardLanAuthOptions.DevelopmentPassword);
    }

    public string Username => options.Value.AdminUsername.Trim();

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static bool VerifyPasswordHash(string password, string passwordHash)
    {
        var parts = passwordHash.Split(':');

        if (parts.Length != 4 ||
            !string.Equals(parts[0], "pbkdf2-sha256", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations < 100_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool SecureEquals(string left, string right)
    {
        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));

        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
