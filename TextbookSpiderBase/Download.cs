using CSharpFunctionalExtensions;
using Serilog;

namespace TextbookSpider;

public static class Download
{
    public delegate ValueTask<Result<Stream>> GetStreamAsyncDelegate(string url, CancellationToken token);

    public static async Task DownloadBookAsync(
        DownloadMethods downloadMethods,
        DirectoryInfo directoryInfo,
        Book book
    )
    {
        var path = Path.Combine(directoryInfo.FullName, book.Title + ".pdf");
        Log.Information("Downloading {BookTitle}", book.Title);
        var urlResult = await Fetch.FetchUrlAsync(book.Id, downloadMethods.GetStringAsync, CancellationToken.None);
        if (urlResult.IsFailure)
        {
            Log.Error("Failed to get download URL for {BookTitle}: {Error}", book.Title, urlResult.Error);
            return;
        }

        var url = urlResult.Value;
        var streamResult = await downloadMethods.GetStreamAsync(url, CancellationToken.None);
        streamResult.Match(
            stream =>
            {
                using var fileStream = new FileStream(path, FileMode.Create);
                stream.CopyTo(fileStream);
                stream.Dispose();
                Log.Information("Downloaded {BookTitle}", book.Title);
            },
            error => Log.Error("Failed to download {BookTitle}: {Error}", book.Title, error)
        );
    }

    public static async Task DownloadFolderAsync(
        DownloadMethods downloadMethods,
        DirectoryInfo directoryInfo,
        Folder folder
    )
    {
        var currentDirectory = directoryInfo.CreateSubdirectory(folder.Name.GetValueOrDefault("Unnamed"));
        if (folder.Subfolders.Any())
        {
            foreach (var subfolder in folder.Subfolders)
            {
                await DownloadFolderAsync(downloadMethods, currentDirectory, subfolder);
            }
        }
        else
        {
            foreach (var book in folder.Books)
            {
                await DownloadBookAsync(downloadMethods, currentDirectory, book);
            }
        }
    }
}