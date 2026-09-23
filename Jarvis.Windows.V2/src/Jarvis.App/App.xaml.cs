using Microsoft.UI.Xaml;

namespace Jarvis.App;

public partial class App : Application
{
    private Window? mainWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        mainWindow = new MainWindow();
        mainWindow.Activate();
    }
}
