using System.ComponentModel;
using System.Windows;

namespace StorePos.Desktop.Startup;

public partial class StartupWindow : Window
{
    private bool _allowClose;

    public StartupWindow()
    {
        InitializeComponent();
    }

    public event EventHandler? RetryRequested;

    public event EventHandler? CloseRequested;

    public void ShowWaiting()
    {
        StatusText.Text = "სერვისთან დაკავშირება...";
        LoadingIndicator.Visibility = Visibility.Visible;
        FailureActions.Visibility = Visibility.Collapsed;
    }

    public void ShowFailure()
    {
        StatusText.Text = "StorePos სერვისთან დაკავშირება ვერ მოხერხდა.";
        LoadingIndicator.Visibility = Visibility.Collapsed;
        FailureActions.Visibility = Visibility.Visible;
    }

    public void CloseAfterStartup()
    {
        _allowClose = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            _allowClose = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        base.OnClosing(e);
    }

    private void OnRetryClick(object sender, RoutedEventArgs e)
        => RetryRequested?.Invoke(this, EventArgs.Empty);

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        _allowClose = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
