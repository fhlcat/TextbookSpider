using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Serilog.Events;

namespace TextbookSpiderWpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
///
[ObservableObject]
public partial class MainWindow
{
    public FoldersViewmodel FoldersViewmodel { get; } = new();

    public MainViewmodel MainViewmodel { get; } = new();

    private readonly ObservableCollection<LogEvent> _logs = [];

    public string LogsText => string.Join(Environment.NewLine, _logs.Select(logEvent => logEvent.RenderMessage()));

    public MainWindow()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.ObservableCollection(
                _logs,
                Application.Current.Dispatcher.Invoke,
                options =>
                {
                    options.MaxStoredEvents = 1000;
                    options.MinimumLevel = LogEventLevel.Information;
                }
            )
            .WriteTo.File("log.txt").CreateLogger();

        _logs.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(LogsText)); };

        Loaded += (_, _) =>
        {
            if (FoldersViewmodel.StartFetchingFoldersCommand.CanExecute(null))
            {
                FoldersViewmodel.StartFetchingFoldersCommand.Execute(null);
            }
        };

        InitializeComponent();
    }
}