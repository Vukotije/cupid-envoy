using System.Security.Cryptography;

namespace ChaoticCupid.Server.Helpers;

/// <summary>
/// Cryptographically strong random helper used by the scoring algorithm.
///
/// The specification asks for <c>RNGCryptoServiceProvider</c>, but that type is
/// obsolete (SYSLIB0023) since .NET 6. <see cref="RandomNumberGenerator"/> is the
/// supported cryptographic RNG and is used here instead. System.Random is never used.
/// </summary>
public static class CryptoRandom
{
    /// <summary>Returns a cryptographically random int in [minInclusive, maxExclusive).</summary>
    public static int GetInt32(int minInclusive, int maxExclusive)
        => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
}
