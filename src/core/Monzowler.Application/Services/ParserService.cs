using System.Net;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;

namespace Monzowler.Application.Services;

/// <summary>
/// This class will try to run several parsers if one of them fails. It is likely that some domains are JS-Heavy,
/// which might require us to use a headless browser and parse from it.
/// If that is the case we will fall back to this instead. 
/// </summary>
public class ParserService(IEnumerable<ISubParser> parsers, ILogger<ParserService> logger) : IParser
{
    private readonly List<ISubParser> _parsers = parsers.ToList();
    public async Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct)
    {
        foreach (var parser in _parsers)
        {
            try
            {
                var response = await parser.ParseLinksAsync(request, ct);
                
                if (response.Links.Count > 0)
                {
                    return new ParserResponse
                    {
                        Links = response.Links,
                        StatusCode = ParserStatusCode.Ok
                    };
                }
        
                return new ParserResponse
                {
                    Links = new(),
                    StatusCode = ParserStatusCode.NoLinksFound
                };

            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning("Http Exception occured for {Url}: {Status}", request.Url, ex.StatusCode);
                
                //TODO: Add more status codes in the future 
                var status = ex.StatusCode switch
                {
                    HttpStatusCode.RequestTimeout =>
                        ParserStatusCode.TimeoutError,
                    HttpStatusCode.NotFound =>
                        ParserStatusCode.NotFoundError,
                    >= HttpStatusCode.InternalServerError and < HttpStatusCode.NetworkAuthenticationRequired
                        => ParserStatusCode.ServerError,
                    _ => ParserStatusCode.HttpError
                };

                return new ParserResponse
                {
                    Links = [],
                    StatusCode = status
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ParserService {ParserService} failed for {Url}",
                    parser.GetType().Name, request.Url);
            }
        }

        logger.LogWarning("All parsers failed for {Url}", request.Url);
        return new ParserResponse
        {
            Links = [],
            StatusCode = ParserStatusCode.ParserError
        };
    }
}