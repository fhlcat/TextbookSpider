// See https://aka.ms/new-console-template for more information

using System.Text;
using Serilog;
using TextbookSpider;

namespace TextbookSpiderCli;

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = new UTF8Encoding();
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("log.txt")
            .CreateLogger();

        var auth = Tools.AskForString("Enter your auth: ", s => !string.IsNullOrWhiteSpace(s));
        var downloadMethods = DownloadMethods.ConstructDownloadMethods(auth);
        Log.Information("Fetching folder information");
        var folder = await Fetch.FetchFolderAsync(downloadMethods.GetStringAsync, CancellationToken.None);
        var selectedFolder = Tools.ChooseFolder(folder.Value);
        Log.Information("Fetching files");
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        await Download.DownloadFolderAsync(downloadMethods, currentDirectory, selectedFolder);
    }
}