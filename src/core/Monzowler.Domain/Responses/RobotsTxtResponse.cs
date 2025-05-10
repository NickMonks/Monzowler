namespace Monzowler.Crawler.Models;

public class RobotsTxtResponse
{
    public List<string> Disallows { get; set; }
    public List<string> Allows { get; set; }
    public int Delay { get; set; }
}