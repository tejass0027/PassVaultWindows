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

        // Some remote-desktop/VM sessions don't hand the newly-created window OS keyboard
        // focus on launch, leaving mouse clicks working but the keyboard silently going
        // nowhere. Forcing activation (with the Topmost toggle trick, which works around
        // Windows' focus-stealing prevention) fixes that without affecting a normal launch.
        mainWindow.Activate();
        mainWindow.Topmost = true;
        mainWindow.Topmost = false;
        mainWindow.Focus();
    }
}
