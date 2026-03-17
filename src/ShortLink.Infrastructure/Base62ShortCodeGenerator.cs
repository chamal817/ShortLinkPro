using ShortLink.Domain;

namespace ShortLink.Infrastructure;

/// <summary>
/// Generates URL-safe short codes using Base62 (0-9, A-Z, a-z).
/// Uses random bytes; collision handling is done by the caller (retry on unique constraint).
/// </summary>
public sealed class Base62ShortCodeGenerator : IShortCodeGenerator
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Length = 8;

    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = new byte[Length];
        Random.Shared.NextBytes(bytes);
        var chars = new char[Length];
        for (var i = 0; i < Length; i++)
            chars[i] = Base62Chars[bytes[i] % Base62Chars.Length];
        return Task.FromResult(new string(chars));
    }
}
