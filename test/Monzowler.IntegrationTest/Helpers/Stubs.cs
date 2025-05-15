using Monzowler.Domain.Entities;

namespace Monzowler.IntegrationTest.Helpers;

public static class Stubs
{
    public static List<Page> ExpectedStaticSiteMap(string baseUrl) =>
    [
        new()
        {
            PageUrl = $"{baseUrl}/",
            Status = nameof(ParserStatusCode.Ok),
            Links = [$"{baseUrl}/page2_level1", $"{baseUrl}/page3_level1"]
        },

        new()
        {
            PageUrl = $"{baseUrl}/page2_level1",
            Status = nameof(ParserStatusCode.NoLinksFound),
            Links = []
        },

        new()
        {
            PageUrl = $"{baseUrl}/page3_level1",
            Status = nameof(ParserStatusCode.Ok),
            Links = [$"{baseUrl}/page4_level2"]
        },

        new()
        {
            PageUrl = $"{baseUrl}/page4_level2",
            Status = nameof(ParserStatusCode.NoLinksFound),
            Links = []
        }

    ];
    
    public static List<Page> ExpectedRenderedSiteMap(string baseUrl) =>  new List<Page>
    {
        new()
        {
            PageUrl = $"{baseUrl}/rendered",
            Status = nameof(ParserStatusCode.Ok),
            Links = [$"{baseUrl}/page2_rendered",$"{baseUrl}/page3_rendered"]
        },
        new()
        {
            PageUrl = $"{baseUrl}/page2_rendered",
            Status = nameof(ParserStatusCode.NoLinksFound),
            Links = []
        },
        new()
        {
            PageUrl = $"{baseUrl}/page3_rendered",
            Status = nameof(ParserStatusCode.Ok),
            Links = [$"{baseUrl}/page4_rendered"]
        },
        new()
        {
            PageUrl = $"{baseUrl}/page4_rendered",
            Status = nameof(ParserStatusCode.NoLinksFound),
            Links = []
        },
    };
    
    public static List<Page> ExpectedSiteMapWithRobotsTxt(string baseUrl) =>
    [
        new()
        {
            PageUrl = $"{baseUrl}/with-robots-txt",
            Status = nameof(ParserStatusCode.Ok),
            Links = [$"{baseUrl}/page2_with-robots-txt",$"{baseUrl}/disallow"]
        },
        
        new()
        {
            PageUrl = $"{baseUrl}/disallow",
            Status = nameof(ParserStatusCode.Disallowed),
            Links = []
        },

        new()
        {
            PageUrl = $"{baseUrl}/page2_with-robots-txt",
            Status = nameof(ParserStatusCode.NoLinksFound),
            Links = []
        }
    ];
}