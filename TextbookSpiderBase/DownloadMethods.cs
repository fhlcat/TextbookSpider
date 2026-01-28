using CSharpFunctionalExtensions;
using Polly;
using Polly.Retry;

namespace TextbookSpider;

public record struct DownloadMethods
{
    public required Download.GetStreamAsyncDelegate GetStreamAsync { get; init; }
    public required Fetch.GetStringAsyncDelegate GetStringAsync { get; init; }
        
    private static readonly HttpClient Client = new();
    private static readonly AsyncRetryPolicy Policy = Polly.Policy.Handle<HttpRequestException>().RetryAsync(3);

    private static Fetch.GetStringAsyncDelegate ConstructGetStringAsync(string auth)
    {
        return async (url, ct) =>
            await Result.Try(async () =>
                await Policy.ExecuteAsync(async () =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Add("x-nd-auth", auth);

                        var response = await Client.SendAsync(request, ct);
                        response.EnsureSuccessStatusCode();

                        return await response.Content.ReadAsStringAsync(ct);
                    }
                )
            );
    }
    
    private static Download.GetStreamAsyncDelegate ConstructGetStreamAsync(string auth)
    {
        return async (url, ct) =>
            await Result.Try(async () =>
                await Policy.ExecuteAsync(async () =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Add("x-nd-auth", auth);

                        var response = await Client.SendAsync(request, ct);
                        response.EnsureSuccessStatusCode();

                        return await response.Content.ReadAsStreamAsync(ct);
                    }
                )
            );
    }

    public static DownloadMethods ConstructDownloadMethods(string auth)
    {
        return new DownloadMethods
        {
            GetStreamAsync = ConstructGetStreamAsync(auth),
            GetStringAsync = ConstructGetStringAsync(auth)
        };
    }
}