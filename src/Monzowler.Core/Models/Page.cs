namespace Monzowler.Crawler.Models;

public class Page
{
    public string Domain {get; set;}
    public string PageUrl {get; set;}
    public List<string> Links {get; set;}
    public string? Status { get; set; }
    public string LastModified {get; set;}
    public string? JobId {get; set;}
}