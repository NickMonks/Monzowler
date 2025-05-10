namespace Monzowler.Crawler.Models;

public class Page
{
    public string Domain {get; init;} = null!;
    public string PageUrl {get; init;} = null!;
    public int Depth {get; init;}
    public List<string> Links {get; init;} = null!;
    public string? Status { get; init; }
    public string LastModified {get; init;} = null!;
    public string? JobId {get; init;}
}