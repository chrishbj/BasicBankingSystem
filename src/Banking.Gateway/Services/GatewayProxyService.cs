using System.Net.Http.Headers;

namespace Banking.Gateway.Services;

public sealed class GatewayProxyService(IHttpClientFactory httpClientFactory)
{
    public async Task ProxyAsync(
        HttpContext httpContext,
        string clientName,
        string? downstreamPath,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(clientName);
        var targetUri = BuildTargetUri(client.BaseAddress, downstreamPath, httpContext.Request.QueryString);

        using var downstreamRequest = new HttpRequestMessage(new HttpMethod(httpContext.Request.Method), targetUri);
        CopyRequestHeaders(httpContext.Request, downstreamRequest);

        if (httpContext.Request.ContentLength > 0 || httpContext.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            downstreamRequest.Content = new StreamContent(httpContext.Request.Body);
            if (!string.IsNullOrWhiteSpace(httpContext.Request.ContentType))
            {
                downstreamRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(httpContext.Request.ContentType);
            }
        }

        using var downstreamResponse = await client.SendAsync(
            downstreamRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        httpContext.Response.StatusCode = (int)downstreamResponse.StatusCode;
        CopyResponseHeaders(downstreamResponse, httpContext.Response);

        await using var responseStream = await downstreamResponse.Content.ReadAsStreamAsync(cancellationToken);
        await responseStream.CopyToAsync(httpContext.Response.Body, cancellationToken);
    }

    private static Uri BuildTargetUri(Uri? baseAddress, string? downstreamPath, QueryString queryString)
    {
        if (baseAddress is null)
        {
            throw new InvalidOperationException("Proxy client base address is not configured.");
        }

        var relativePath = string.IsNullOrWhiteSpace(downstreamPath) ? string.Empty : downstreamPath.TrimStart('/');
        var builder = new UriBuilder(new Uri(baseAddress, relativePath));
        builder.Query = queryString.HasValue ? queryString.Value?.TrimStart('?') : string.Empty;
        return builder.Uri;
    }

    private static void CopyRequestHeaders(HttpRequest sourceRequest, HttpRequestMessage targetRequest)
    {
        foreach (var header in sourceRequest.Headers)
        {
            if (ShouldSkipRequestHeader(header.Key))
            {
                continue;
            }

            if (!targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                targetRequest.Content ??= new ByteArrayContent([]);
                targetRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private static void CopyResponseHeaders(HttpResponseMessage sourceResponse, HttpResponse targetResponse)
    {
        foreach (var header in sourceResponse.Headers)
        {
            targetResponse.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in sourceResponse.Content.Headers)
        {
            targetResponse.Headers[header.Key] = header.Value.ToArray();
        }

        targetResponse.Headers.Remove("transfer-encoding");
    }

    private static bool ShouldSkipRequestHeader(string headerName) =>
        headerName.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
        headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase);
}
