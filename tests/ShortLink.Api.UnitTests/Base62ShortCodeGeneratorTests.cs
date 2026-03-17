using ShortLink.Infrastructure;
using Xunit;

namespace ShortLink.Api.UnitTests;

public class Base62ShortCodeGeneratorTests
{
    private static readonly char[] Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    [Fact]
    public async Task GenerateAsync_Returns_Length8()
    {
        var gen = new Base62ShortCodeGenerator();
        var code = await gen.GenerateAsync();
        Assert.Equal(8, code.Length);
    }

    [Fact]
    public async Task GenerateAsync_Returns_OnlyBase62Characters()
    {
        var gen = new Base62ShortCodeGenerator();
        var code = await gen.GenerateAsync();
        Assert.All(code, c => Assert.Contains(c, Base62Chars));
    }

    [Fact]
    public async Task GenerateAsync_Respects_CancellationToken()
    {
        var gen = new Base62ShortCodeGenerator();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => gen.GenerateAsync(cts.Token));
    }

    [Fact]
    public async Task GenerateAsync_Produces_DifferentCodes_WhenCalledMultipleTimes()
    {
        var gen = new Base62ShortCodeGenerator();
        var codes = new HashSet<string>();
        for (var i = 0; i < 100; i++)
            codes.Add(await gen.GenerateAsync());
        Assert.True(codes.Count > 1, "Multiple calls should typically produce different codes");
    }
}
