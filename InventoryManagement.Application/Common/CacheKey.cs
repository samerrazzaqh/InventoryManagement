using System.Security.Cryptography;
using System.Text;

namespace InventoryManagement.Application.Common;

public static class CacheKey
{
    /// <summary>Deterministic, fixed-length key for arbitrary filter combinations (also neutralises hostile search strings).</summary>
    public static string Hash(string prefix, params object?[] parts)
    {
        var raw = string.Join('|', parts.Select(p => p?.ToString() ?? "~"));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..32];
        return $"{prefix}:{hash}";
    }
}
