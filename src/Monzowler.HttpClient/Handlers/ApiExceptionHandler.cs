namespace Monzowler.HttpClient.Handlers;

using Microsoft.Extensions.Logging;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                //TODO: should we do something here for the website - any specific errors we want to handle?
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Request to {Url} was cancelled.", request.RequestUri);
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "An error occurred during the HTTP request.");
            throw;
        }
    }
}