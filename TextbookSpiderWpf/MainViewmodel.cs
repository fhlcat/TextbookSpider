using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSharpFunctionalExtensions;
using Serilog;
using Serilog.Events;
using TextbookSpider;

namespace TextbookSpiderWpf;

public partial class FoldersViewmodel : ObservableObject
{
    public ObservableCollection<Folder> Folders { get; } = [];

    private readonly DownloadMethods _downloadMethods = DownloadMethods.ConstructDownloadMethods(string.Empty);

    private Task? _folderFetchingTask;

    private async Task FetchFolderAsync()
    {
        Log.Information("Fetching folders");
        var fetchFolderResult = await Fetch.FetchFolderAsync(_downloadMethods.GetStringAsync, CancellationToken.None);
        fetchFolderResult.Match(
            folder =>
            {
                Folders.Clear();
                Folders.Add(folder);
                Log.Information("Fetched folders successfully");
            },
            error => Log.Error("Failed to fetch folders: {Error}", error));
    }

    private bool CanStartFetchingFolders() => _folderFetchingTask is null || _folderFetchingTask.IsCompleted;

    [RelayCommand(CanExecute = nameof(CanStartFetchingFolders))]
    private void StartFetchingFolders()
    {
        _folderFetchingTask = FetchFolderAsync();
    }
}

public partial class MainViewmodel : ObservableObject
{
    [ObservableProperty] private string _auth = string.Empty;

    private Task? _workingTask;

    private async Task Work(Folder folder)
    {
        var downloadMethods = DownloadMethods.ConstructDownloadMethods(Auth);
        var currentDirectoryInfo = new DirectoryInfo(
            Path.Combine(Environment.CurrentDirectory, "Textbooks")
        );
        await Download.DownloadFolderAsync(downloadMethods, currentDirectoryInfo, folder);
    }

    private bool CanStartWorking(Folder folder) => _workingTask is null || _workingTask.IsCompleted;

    [RelayCommand(CanExecute = nameof(CanStartWorking))]
    private void StartWorking(Folder folder)
    {
        _workingTask = Work(folder);
    }
}