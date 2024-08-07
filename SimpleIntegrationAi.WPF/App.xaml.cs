﻿using System.Windows;
using AiTestLibrary.Classes;
using AiTestLibrary.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SimpleIntegrationAi.Domain.Services;
using SimpleIntegrationAi.WPF.ViewModels;
using SimpleIntegrationAi.WPF.Views;

namespace SimpleIntegrationAi.WPF;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        IServiceProvider serviceProvider = CreateServiceProvider();
        MainWindow window = new MainWindow
        {
            DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
        };
        window.Show();
        base.OnStartup(e);
    }

    private IServiceProvider CreateServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IResponseParser, ResponseParser>();
        services.AddSingleton<IChatGpt, ChatGpt>();
        services.AddSingleton<IGeminiGpt, GeminiGpt>();

        services.AddScoped<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}