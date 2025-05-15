namespace Monzowler.Domain.Responses;

public class RobotsTxtResponse
{
    public required List<string> Disallows { get; init; }
    public required List<string> Allows { get; init; }
    public int Delay { get; init; }
}