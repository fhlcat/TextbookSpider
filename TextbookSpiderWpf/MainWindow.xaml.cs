using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Serilog;
using Serilog.Events;

namespace TextbookSpiderWpf;

[ObservableObject]
public partial class MainWindow
{
    public MainViewmodel MainViewmodel { get; } = new();
    private readonly ObservableCollection<LogEvent> _logs = [];
    private bool _closing;
    public string LogsText => string.Join(Environment.NewLine,
        _logs.Select(entry => $"{entry.Timestamp:HH:mm:ss} [{entry.Level}] {entry.RenderMessage()}{(entry.Exception is null ? string.Empty : " " + entry.Exception.Message)}"));

    public MainWindow()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.ObservableCollection(_logs, action => Application.Current.Dispatcher.Invoke(action),
                options =>
                {
                    options.MaxStoredEvents = 1000;
                    options.MinimumLevel = LogEventLevel.Information;
                })
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "log.txt")).CreateLogger();
        _logs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(LogsText));
        InitializeComponent();
        Loaded += async (_, _) => await MainViewmodel.StartFetchingFoldersCommand.ExecuteAsync(null);
        Closing += OnClosing;
        Closed += (_, _) => Log.CloseAndFlush();
    }

    private void FolderSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        => MainViewmodel.SelectedFolder = e.NewValue as FolderViewmodel;

    private void AuthChanged(object sender, RoutedEventArgs e)
        => MainViewmodel.Auth = AuthBox.Password;

    private void BrowseFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "选择教材保存文件夹" };
        if (Directory.Exists(MainViewmodel.SavePath)) dialog.InitialDirectory = MainViewmodel.SavePath;
        if (dialog.ShowDialog(this) == true) MainViewmodel.SavePath = dialog.FolderName;
    }

    private void OpenFolder(object sender, RoutedEventArgs e)
    {
        var result = CSharpFunctionalExtensions.Result.Try(() =>
        {
            var directory = Directory.CreateDirectory(MainViewmodel.SavePath.Trim());
            Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { directory.FullName } });
        });
        if (result.IsFailure)
        {
            MainViewmodel.Status = "无法打开保存目录，请检查路径";
            Log.Error("打开保存目录失败：{Error}", result.Error);
        }
    }

    private void ClearLogs(object sender, RoutedEventArgs e) => _logs.Clear();

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!MainViewmodel.IsBusy) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        if (MainViewmodel.CancelCommand.CanExecute(null)) MainViewmodel.CancelCommand.Execute(null);
        var task = MainViewmodel.StartWorkingCommand.ExecutionTask ?? MainViewmodel.StartFetchingFoldersCommand.ExecutionTask;
        // The catalog and download commands are mutually exclusive. Await the active command.
        if (MainViewmodel.StartFetchingFoldersCommand.IsRunning)
            task = MainViewmodel.StartFetchingFoldersCommand.ExecutionTask;
        if (task is not null) await task;
        Close();
    }
}
