using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinaGroupApp;
using MinaGroupApp.Services.Interfaces;
using MinaGroupApp.ViewModels;

namespace MinaGroupApp.ViewModels.Auth
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string errorMessage;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Log ind";
        }

        [RelayCommand]
        public async Task Login()
        {
            IsBusy = true;
            ErrorMessage = "";

            var loginResult = await _authService.LoginAsync(Email, Password);

            if (loginResult)
            {
                if(Application.Current?.MainPage != null)
                    Application.Current.MainPage = new AppShell();
            }
            else
            {
                ErrorMessage = "Der opstod en uventet fejl, prøv venligst igen!";
            }

            IsBusy = false;
        }
    }
}