using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using SE2_Language_Replacer.Lib;

namespace SE2_Language_Replacer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

        ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });

            if (!Design.IsDesignMode)
            {
                try
                {
                    StreamWriter logFileWriter = new StreamWriter(Constants.LogFileName);
                    builder.AddProvider(new FileLoggerProvider(logFileWriter));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

        });
        var log = factory.CreateLogger("Program");
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(log);
        }

        base.OnFrameworkInitializationCompleted();
    }
}