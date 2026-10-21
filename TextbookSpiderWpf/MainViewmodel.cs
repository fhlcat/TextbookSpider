using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSharpFunctionalExtensions;
using Serilog;
using TextbookSpider;

namespace TextbookSpiderWpf;

public sealed class FolderViewmodel
{
    public Folder Folder { get; }
    public string Name => Folder.Name.GetValueOrDefault("全部教材");
    public IReadOnlyList<FolderViewmodel> Subfolders { get; }
    public IReadOnlyList<Book> Books { get; }

    public FolderViewmodel(Folder folder, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Subfolders = folder.Subfolders.Select(child => new FolderViewmodel(child, token)).ToArray();
        Books = (Subfolders.Count == 0 ? folder.Books : Subfolders.SelectMany(child => child.Books))
            .ToArray();
        Folder = folder with
        {
            Subfolders = Subfolders.Select(child => child.Folder).ToArray(),
            Books = Subfolders.Count == 0 ? Books : []
        };
    }
}

public partial class MainViewmodel : ObservableObject
{
    private readonly Func<string, DownloadMethods> _createDownloadMethods;
    public MainViewmodel() : this(DownloadMethods.ConstructDownloadMethods) { }
    public MainViewmodel(Func<string, DownloadMethods> createDownloadMethods) => _createDownloadMethods = createDownloadMethods;
    public ObservableCollection<FolderViewmodel> Folders { get; } = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartWorkingCommand))]
    private string _auth = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartWorkingCommand))]
    private string _savePath = Path.Combine(AppContext.BaseDirectory, "Textbooks");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBooks))]
    [NotifyPropertyChangedFor(nameof(SelectionText))]
    [NotifyCanExecuteChangedFor(nameof(StartWorkingCommand))]
    private FolderViewmodel? _selectedFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(StartFetchingFoldersCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartWorkingCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _status = "正在准备教材目录…";
    [ObservableProperty] private int _completedBooks;
    [ObservableProperty] private int _totalBooks = 1;
    [ObservableProperty] private string _progressText = "尚未开始下载";
    [ObservableProperty] private bool _isLoading;

    private CancellationTokenSource? _cancellation;
    public bool IsIdle => !IsBusy;
    public IReadOnlyList<Book> SelectedBooks => SelectedFolder?.Books ?? [];
    public string SelectionText => SelectedFolder is null ? "请从左侧选择教材分类" : $"{SelectedFolder.Name} · {SelectedBooks.Count} 本教材";
    private bool CanFetch() => !IsBusy;
    private bool CanDownload() => !IsBusy && SelectedBooks.Count > 0 &&
        !string.IsNullOrWhiteSpace(Auth) && !string.IsNullOrWhiteSpace(SavePath);
    private bool CanCancel() => IsBusy && _cancellation is { IsCancellationRequested: false };

    [RelayCommand(CanExecute = nameof(CanFetch))]
    private async Task StartFetchingFoldersAsync()
    {
        BeginOperation();
        IsLoading = true;
        Status = "正在获取教材目录…";
        try
        {
            var methods = _createDownloadMethods(Auth.Trim());
            var result = await Fetch.FetchFolderAsync(methods.GetStringAsync, _cancellation!.Token);
            _cancellation.Token.ThrowIfCancellationRequested();
            if (result.IsFailure)
            {
                Status = "目录加载失败，请查看日志并重试";
                Log.Error("获取目录失败：{Error}", result.Error);
                return;
            }
            // Materialize large lazy catalogs off the UI thread before replacing the tree.
            var token = _cancellation.Token;
            var root = await Task.Run(() => new FolderViewmodel(result.Value, token), token);
            token.ThrowIfCancellationRequested();
            SelectedFolder = null;
            Folders.Clear();
            Folders.Add(root);
            Status = $"目录加载完成，共 {root.Books.Count} 本教材";
            Log.Information("教材目录加载完成，共 {Count} 本", root.Books.Count);
        }
        catch (OperationCanceledException) { Status = "目录加载已取消"; }
        catch (Exception exception)
        {
            Status = "目录加载失败，请查看日志并重试";
            Log.Error(exception, "获取目录失败");
        }
        finally { IsLoading = false; EndOperation(); }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task StartWorkingAsync()
    {
        if (!CanDownload()) return;
        var folder = SelectedFolder!.Folder;
        var pathResult = Result.Try(() => new DirectoryInfo(Path.GetFullPath(SavePath.Trim())));
        if (pathResult.IsFailure)
        {
            Status = "保存路径无效，请重新选择";
            Log.Error("保存路径无效：{Error}", pathResult.Error);
            return;
        }

        BeginOperation();
        CompletedBooks = 0;
        TotalBooks = 1;
        ProgressText = "正在准备下载…";
        Status = "正在下载教材…";
        try
        {
            var progress = new Progress<DownloadProgress>(value =>
            {
                CompletedBooks = value.Completed;
                TotalBooks = value.Total;
                ProgressText = $"{value.Completed} / {value.Total} · 成功 {value.Succeeded} · 失败 {value.Failed}";
            });
            var result = await Download.DownloadFolderAsync(_createDownloadMethods(Auth.Trim()),
                pathResult.Value, folder, _cancellation!.Token, progress);
            Status = result.Failed == 0 ? $"下载完成：成功 {result.Succeeded} 本" : $"下载结束：成功 {result.Succeeded} 本，失败 {result.Failed} 本，请查看日志";
            Log.Information("{Status}", Status);
        }
        catch (OperationCanceledException)
        {
            Status = "下载已取消，已完成的文件已保留";
            Log.Information("下载已取消");
        }
        catch (Exception exception)
        {
            Status = "下载失败，请查看日志";
            Log.Error(exception, "下载任务失败");
        }
        finally { EndOperation(); }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cancellation?.Cancel();
        Status = "正在取消…";
        CancelCommand.NotifyCanExecuteChanged();
    }

    private void BeginOperation()
    {
        _cancellation = new CancellationTokenSource();
        IsBusy = true;
    }

    private void EndOperation()
    {
        _cancellation?.Dispose();
        _cancellation = null;
        IsBusy = false;
    }
}
