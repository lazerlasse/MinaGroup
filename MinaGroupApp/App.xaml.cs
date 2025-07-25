using MinaGroupApp;
using MinaGroupApp.Pages;
using MinaGroupApp.Services.Interfaces;
using MinaGroupApp.ViewModels.Auth;
using System.Diagnostics;

namespace MinaGroupApp;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ISecureStorageService _secureStorageService;

    public App(IServiceProvider services)
    {
        InitializeComponent();

        _services = services;
        _secureStorageService = services.GetRequiredService<ISecureStorageService>();

        // Midlertidig side vises under token-check
        MainPage = new ContentPage
        {
            Content = new ActivityIndicator
            {
                IsRunning = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        // Start async init
        Task.Run(() => InitMainPageAsync());
    }

    private async Task InitMainPageAsync()
    {
        try
        {
            var token = await _secureStorageService.GetAccessTokenAsync();
            var expiry = await _secureStorageService.GetAccessTokenExpiryAsync();

            if (!string.IsNullOrWhiteSpace(token) && expiry.HasValue && expiry.Value > DateTime.UtcNow)
            {
                // Token er gyldig → naviger til AppShell (hvor FrontPage vises)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new AppShell();
                });
            }
            else
            {
                // Token mangler eller er udløbet → vis LoginPage m. korrekt ViewModel via DI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var loginPage = _services.GetRequiredService<LoginPage>();

                    MainPage = new NavigationPage(loginPage);
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App Init Error]: {ex.Message}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new ContentPage
                {
                    Content = new Label
                    {
                        Text = "Fejl under opstart af appen.",
                        TextColor = Colors.Red,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
            });
        }
    }
}