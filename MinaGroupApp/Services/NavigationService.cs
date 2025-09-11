using MinaGroupApp.Pages;
using MinaGroupApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _services;

        public NavigationService(IServiceProvider services)
        {
            _services = services;
        }

        public async Task NavigateToAsync(string route, bool clearStack = false)
        {
            if (clearStack)
                await Shell.Current.GoToAsync(route, true);
            else
                await Shell.Current.GoToAsync(route);
        }

        public Task NavigateToLoginAsync()
        {
            // 1. Vis midlertidig loader
            if(Application.Current?.MainPage != null)
                Application.Current.MainPage = new NavigationPage(new LoadingPage());

            // Denne side ligger uden for Shell
            var loginPage = _services.GetRequiredService<LoginPage>();

            if(Application.Current?.MainPage != null)
                Application.Current.MainPage = new NavigationPage(loginPage);
            
            return Task.CompletedTask;
        }
    }
}
