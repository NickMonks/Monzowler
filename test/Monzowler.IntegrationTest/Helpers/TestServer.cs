using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Monzowler.IntegrationTest.Helpers;

public class TestServer : IDisposable
{
    private readonly WireMockServer _server;

    public string BaseUrl => _server.Urls[0];

    public TestServer()
    {
        _server = WireMockServer.Start();
        RegisterStubs();
    }

    private void RegisterStubs()
    {
        string type;
        //static htmls
        RegisterPage("/", "page1_level0.html");
        RegisterPage("/page2_level1", "page2_level1.html");
        RegisterPage("/page3_level1", "page3_level1.html");
        RegisterPage("/page4_level2", "page4_level2.html");
        
        //rendered
        type = "rendered";
        RegisterPage($"/{type}", "page1.html", type);
        RegisterPage($"/page2_{type}", "page2.html", type);
        RegisterPage($"/page3_{type}", "page3.html", type);
        RegisterPage($"/page4_{type}", "page4.html", type);
        
        //robots txt
        type = "with-robots-txt";
        RegisterPage($"/{type}", "page1.html", type);
        RegisterPage($"/page2_{type}", "page2.html", type);
        RegisterPage($"/disallow", "disallow.html", type);
        RegisterPage($"/robots.txt", "robots.txt", type);
    }
    
    private void RegisterPage(string route, string fileName, string type = "static")
    {
        var basePath = Directory.GetCurrentDirectory();
        var fullPath = Path.Combine(basePath, "Helpers", "templates", type, fileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"HTML file not found for route {route}: {fileName}");
        }

        var htmlContent = File.ReadAllText(fullPath);

        _server.Given(Request.Create()
                .WithPath(route)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithHeader("Content-Type", "text/html")
                .WithBody(htmlContent)
                .WithStatusCode(200));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}