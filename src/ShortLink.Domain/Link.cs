namespace ShortLink.Domain;

public class Link
{
    public long Id { get; set; }
    public required string ShortCode { get; set; }
    public required string LongUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
