using System.Security.Cryptography;
using System.Text;

namespace Pegasus.Desktop.Infrastructure.Authentication;

public sealed class DpapiCredentialStore(string storeRoot) : IDesktopCredentialStore
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private readonly string _storeRoot = ValidateStoreRoot(storeRoot);

    public void Save(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Directory.CreateDirectory(_storeRoot);

        var plaintext = StrictUtf8.GetBytes(value);
        var protectedValue = ProtectedData.Protect(
            plaintext,
            optionalEntropy: null,
            DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetPath(key), protectedValue);
    }

    public bool TryRead(string key, out string? value)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
        {
            value = null;
            return false;
        }

        try
        {
            var protectedValue = File.ReadAllBytes(path);
            value = StrictUtf8.GetString(ProtectedData.Unprotect(
                protectedValue,
                optionalEntropy: null,
                DataProtectionScope.CurrentUser));
            return true;
        }
        catch (CryptographicException exception)
        {
            throw new InvalidDataException("The credential store entry is invalid.", exception);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("The credential store entry is not valid UTF-8.", exception);
        }
    }

    public void Clear(string key)
    {
        File.Delete(GetPath(key));
    }

    private static string ValidateStoreRoot(string storeRoot)
    {
        if (string.IsNullOrWhiteSpace(storeRoot))
        {
            throw new ArgumentException("A credential store root is required.", nameof(storeRoot));
        }

        return Path.GetFullPath(storeRoot);
    }

    private string GetPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A credential key is required.", nameof(key));
        }

        var keyHash = Convert.ToHexString(SHA256.HashData(StrictUtf8.GetBytes(key)));
        return Path.Combine(_storeRoot, $"{keyHash}.bin");
    }
}
