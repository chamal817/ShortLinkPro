namespace ShortLink.Domain;

public interface IShortCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
