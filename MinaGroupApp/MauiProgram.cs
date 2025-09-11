using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MinaGroupApp.Pages;
using MinaGroupApp.Services;
using MinaGroupApp.Services.Http;
using MinaGroupApp.Services.Interfaces;
using MinaGroupApp.ViewModels;
using MinaGroupApp.ViewModels.Auth;
using Refit;

namespace MinaGroupApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Add Pages.
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<FrontPage>();
            builder.Services.AddTransient<PostSelfEvaluationPage>();
            builder.Services.AddSingleton<SelfEvaluationMainPage>();

            // Add Viewmodels.
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddSingleton<FrontPageViewModel>();
            builder.Services.AddTransient<PostSelfEvaluationViewModel>();
            builder.Services.AddSingleton<SelfEvaluationMainPageViewModel>();

            // Add Services
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
            builder.Services.AddSingleton<StartupService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();

            // Register custom handler for JWT
            builder.Services.AddTransient<AuthHttpMessageHandler>();


            // Add Authentication Refit Client.
            builder.Services.AddRefitClient<IAuthApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = DeviceInfo.Platform == DevicePlatform.Android
                        ? new Uri("http://10.0.2.2:5000")
                        : new Uri("http://localhost:5000");
                });

            // Add Secure Api for authenticated api calls.
            builder.Services.AddRefitClient<ISecureApi>() // Dette interface skal du oprette
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = DeviceInfo.Platform == DevicePlatform.Android
                        ? new Uri("http://10.0.2.2:5000")
                        : new Uri("http://localhost:5000");
                })
                .AddHttpMessageHandler<AuthHttpMessageHandler>();

            // Add User Api for user related api calls.
            builder.Services.AddRefitClient<IUserApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = DeviceInfo.Platform == DevicePlatform.Android
                        ? new Uri("http://10.0.2.2:5000")
                        : new Uri("http://localhost:5000");
                })
                .AddHttpMessageHandler<AuthHttpMessageHandler>();

            // Add Self Evaluation Api for self evaluation related api calls.
            builder.Services.AddRefitClient<ISelfEvaluationApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = DeviceInfo.Platform == DevicePlatform.Android
                        ? new Uri("http://10.0.2.2:5000")
                        : new Uri("http://localhost:5000");
                })
                .AddHttpMessageHandler<AuthHttpMessageHandler>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
