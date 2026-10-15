using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaGUI.Models;
using Garethp.ModsOfMistriaInstallerLib;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

internal enum Pages
{
    GettingStarted,
    Modlist
}

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Settings _settings = Settings.Load();

    private GameRestartMonitor? _restartMonitor;

    [ObservableProperty] private PageViewBase _currentPage;

    public MainWindowViewModel()
    {
        if (!_settings.ValidMistriaLocation())
            _settings.MistriaLocation = MistriaLocator.GetMistriaLocation() ?? "";
        if (!_settings.ValidModsLocation())
            _settings.ModsLocation = MistriaLocator.GetModsLocation(_settings.MistriaLocation) ?? "";

        var gettingStarted = new GettingStartedPageViewModel(_settings);
        var modlist = new ModlistPageViewModel(_settings)
        {
            ReconfigureRequested = () =>
            {
                gettingStarted.IsReconfiguring = true;
                CurrentPage = gettingStarted;
            }
        };

        gettingStarted.Finished = () =>
        {
            gettingStarted.IsReconfiguring = false;
            _settings.Save();
            CurrentPage = modlist;
            StartRestartMonitor();
        };

        var pages = new Dictionary<Pages, PageViewBase>
        {
            { Pages.GettingStarted , gettingStarted },
            { Pages.Modlist, modlist }
        };

        if (!_settings.ValidMistriaLocation() || !_settings.ValidModsLocation())
        {
            CurrentPage = pages[Pages.GettingStarted];
        }
        else
        {
            CurrentPage = pages[Pages.Modlist];
            _settings.Save();
        }

        _settings.PropertyChanged += (_, _) =>
        {
            if (!_settings.ValidMistriaLocation() || !_settings.ValidModsLocation()) return;
            _settings.Save();
            if (gettingStarted.IsReconfiguring) return;
            CurrentPage = pages[Pages.Modlist];
            StartRestartMonitor();
        };

        StartRestartMonitor();
    }

    private void StartRestartMonitor()
    {
        _restartMonitor?.Stop();
        if (string.IsNullOrEmpty(_settings.MistriaLocation)) return;
        _restartMonitor = new GameRestartMonitor(_settings.MistriaLocation);
        _restartMonitor.Start();
    }
}