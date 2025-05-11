namespace Monzowler.Shared.Utilities;

public static class Sanitizer
{
    public static string? SanitizeUrl(string href, string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#"))
                return null;

            var candidateUri = new Uri(new Uri(url), href);

            if (candidateUri.Scheme != Uri.UriSchemeHttp && candidateUri.Scheme != Uri.UriSchemeHttps)
                return null;

            var path = candidateUri.AbsolutePath.ToLowerInvariant();
            var excludedExtensions = new[]
            {
                ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar",
                ".jpg", ".png", ".gif", ".mp4", ".mp3", ".m4a"
            };

            if (excludedExtensions.Any(ext => path.EndsWith(ext)))
                return null;

            return candidateUri.ToString().TrimEnd('/');
        }
        catch
        {
            //href could not be sanitised nor parser correctly
            return null;
        }
        
    }
}