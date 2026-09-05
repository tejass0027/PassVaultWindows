using System.Windows;

namespace PassVaultWindows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appState = new AppState();
        var mainWindow = new MainWindow(appState);
        mainWindow.Show();
    }
}
