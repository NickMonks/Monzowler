namespace Monzowler.Unittest.Helpers;

public static class Helper
{
    public static string LoadTestFile(string fileName)
    {
        var basePath = Directory.GetCurrentDirectory(); 
        var fullPath = Path.Combine(basePath, "Helpers", "RobotsTxtFiles", fileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Could not find test file: {fullPath}");

        return File.ReadAllText(fullPath);
    }
    
    public static string LoadStaticHtlm(string fileName)
    {
        var basePath = Directory.GetCurrentDirectory(); 
        var fullPath = Path.Combine(basePath, "Helpers", "ParserFiles","StaticHtmls", fileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Could not find test file: {fullPath}");

        return File.ReadAllText(fullPath);
    }
    
    public static string LoadRenderedHtlm(string fileName)
    {
        var basePath = Directory.GetCurrentDirectory(); 
        var fullPath = Path.Combine(basePath, "Helpers", "ParserFiles","RenderedHtmls", fileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Could not find test file: {fullPath}");

        return File.ReadAllText(fullPath);
    }
}