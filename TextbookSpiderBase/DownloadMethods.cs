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

    private static HttpRequestMessage CreateRequest(string url, string auth)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(auth)) request.Headers.Add("x-nd-auth", auth);
        return request;
    }

    public static DownloadMethods ConstructDownloadMethods(string auth)
    {
        return new DownloadMethods
        {
            GetStringAsync = async (url, token) => await Result.Try(async () =>
                await Policy.ExecuteAsync(async ct =>
                {
                    using var request = CreateRequest(url, auth);
                    using var response = await Client.SendAsync(request, ct);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync(ct);
                }, token)),
            GetStreamAsync = async (url, token) => await Result.Try<Stream>(async () =>
                await Policy.ExecuteAsync<Stream>(async ct =>
                {
                    using var request = CreateRequest(url, auth);
                    var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        return new ResponseStream(await response.Content.ReadAsStreamAsync(ct), response);
                    }
                    catch
                    {
                        response.Dispose();
                        throw;
                    }
                }, token))
        };
    }

    // Disposing a download stream must also release its HTTP response/connection.
    private sealed class ResponseStream(Stream inner, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => inner.CopyToAsync(destination, bufferSize, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        protected override void Dispose(bool disposing)
        {
            if (disposing) { inner.Dispose(); response.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
