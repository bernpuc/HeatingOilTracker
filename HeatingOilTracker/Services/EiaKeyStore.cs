using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HeatingOilTracker.Services;

/// <summary>
/// Stores and retrieves the EIA API key using Windows DPAPI (user-scoped encryption).
/// Key is encrypted at rest in %APPDATA%\HeatingOilTracker\eia.dat
/// </summary>
public static class EiaKeyStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HeatingOilTracker", "eia.dat");

    public static string Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return string.Empty;

            var encrypted = File.ReadAllBytes(FilePath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EiaKeyStore.Load failed: {ex.Message}");
            return string.Empty;
        }
    }

    public static void Save(string apiKey)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);

        var plaintext = Encoding.UTF8.GetBytes(apiKey);
        var encrypted = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, encrypted);
    }

    public static void Delete()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}
