using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace GuardLan.Api.Auth;

public sealed class InternalPublisherKeyValidator(
    IOptions<GuardLanAuthOptions> options,
    IHostEnvironment environment)
{
    public bool IsValid(string? suppliedKey)
    {
        var expectedKey = options.Value.InternalPublisherKey;

        if (string.IsNullOrWhiteSpace(expectedKey) && environment.IsDevelopment())
        {
            expectedKey = GuardLanAuthOptions.DevelopmentInternalPublisherKey;
        }

        return !string.IsNullOrWhiteSpace(suppliedKey) &&
               !string.IsNullOrWhiteSpace(expectedKey) &&
               SecureEquals(suppliedKey.Trim(), expectedKey.Trim());
    }

    private static bool SecureEquals(string left, string right)
    {
        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));

        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
