using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SE2_Language_Replacer;

public partial class FailDialog : Window
{
    public FailDialog()
    {
        InitializeComponent();
    }
    
    private void Log_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(Constants.LogFileName){ UseShellExecute = true });
    }

    private void Url_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(Constants.GithubLink)
            { UseShellExecute = true });
    }
}