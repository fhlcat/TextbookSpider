using CSharpFunctionalExtensions;
using Serilog;

namespace TextbookSpider;

public sealed record DownloadProgress(int Completed, int Total, int Succeeded, int Failed);
public sealed record DownloadSummary(int Succeeded, int Failed);

public static class Download
{
    public delegate ValueTask<Result<Stream>> GetStreamAsyncDelegate(string url, CancellationToken token);

    // Optional parameters keep the console frontend's existing calls valid.
    public static async Task<Result> DownloadBookAsync(
        DownloadMethods downloadMethods, DirectoryInfo directoryInfo, Book book,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Log.Information("Downloading {BookTitle}", book.Title);
        var urlResult = await Fetch.FetchUrlAsync(book.Id, downloadMethods.GetStringAsync, token);
        token.ThrowIfCancellationRequested();
        if (urlResult.IsFailure) return Fail(book, urlResult.Error);
        var streamResult = await downloadMethods.GetStreamAsync(urlResult.Value, token);
        if (streamResult.IsFailure)
        {
            token.ThrowIfCancellationRequested();
            return Fail(book, streamResult.Error);
        }

        await using var stream = streamResult.Value;
        string? temporaryPath = null;
        var result = await Result.Try(async () =>
        {
            token.ThrowIfCancellationRequested();
            directoryInfo.Create();
            var path = Path.Combine(directoryInfo.FullName, SafeName(book.Title) + ".pdf");
            temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".part";
            try
            {
                await using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                                 FileShare.None, 81920, useAsync: true))
                    await stream.CopyToAsync(file, token);
                token.ThrowIfCancellationRequested();
                File.Move(temporaryPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        });
        token.ThrowIfCancellationRequested();
        if (result.IsFailure) return Fail(book, result.Error);
        Log.Information("Downloaded {BookTitle}", book.Title);
        return Result.Success();
    }

    private static Result Fail(Book book, string error)
    {
        Log.Error("Failed to download {BookTitle}: {Error}", book.Title, error);
        return Result.Failure(error);
    }

    internal static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray())
            .Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(safe)) safe = "Unnamed";
        // Windows reserves these names even when a file extension is present.
        var stem = safe.Split('.')[0];
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }
            .Contains(stem, StringComparer.OrdinalIgnoreCase)) safe = "_" + safe;
        return safe.Length > 120 ? safe[..120].TrimEnd('.') : safe;
    }

    public static async Task<DownloadSummary> DownloadFolderAsync(
        DownloadMethods downloadMethods, DirectoryInfo directoryInfo, Folder folder,
        CancellationToken token = default, IProgress<DownloadProgress>? progress = null)
    {
        // Materialize once: Folder.MapTree exposes lazy child and book enumerations.
        var jobs = EnumerateBooks(directoryInfo, folder).ToArray();
        var succeeded = 0;
        var failed = 0;
        progress?.Report(new DownloadProgress(0, jobs.Length, 0, 0));
        foreach (var (directory, book) in jobs)
        {
            token.ThrowIfCancellationRequested();
            var result = await DownloadBookAsync(downloadMethods, directory, book, token);
            if (result.IsSuccess) succeeded++; else failed++;
            progress?.Report(new DownloadProgress(succeeded + failed, jobs.Length, succeeded, failed));
        }
        return new DownloadSummary(succeeded, failed);
    }

    private static IEnumerable<(DirectoryInfo Directory, Book Book)> EnumerateBooks(DirectoryInfo parent, Folder folder)
    {
        var directory = new DirectoryInfo(Path.Combine(parent.FullName, SafeName(folder.Name.GetValueOrDefault("Unnamed"))));
        var children = folder.Subfolders.ToArray();
        if (children.Length > 0)
        {
            foreach (var child in children)
            foreach (var job in EnumerateBooks(directory, child)) yield return job;
        }
        else
        {
            foreach (var book in folder.Books) yield return (directory, book);
        }
    }
}
