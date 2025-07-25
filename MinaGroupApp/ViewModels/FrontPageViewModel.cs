using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinaGroupApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.ViewModels
{
    public partial class FrontPageViewModel : BaseViewModel
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly INavigationService _navigation;
        private readonly IUserApi _userApi;

        [ObservableProperty]
        private string welcomeMessage;

        public FrontPageViewModel(ISecureStorageService secureStorage, INavigationService navigation, IUserApi userApi)
        {
            _secureStorage = secureStorage;
            _navigation = navigation;
            Title = "Overblik";
            WelcomeMessage = string.Empty;
            _userApi = userApi;
        }

        public async Task InitAsync()
        {
            try
            {
                var userData = await _userApi.GetProfileAsync();
                WelcomeMessage = $"Hej {userData.DisplayName}";
            }
            catch (Exception ex)
            {
                WelcomeMessage = "Bruger blev ikke indlæst!";
                Debug.WriteLine("ERROR: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            await _secureStorage.ClearAllAsync();
            await _navigation.NavigateToAsync("//LoginPage", clearStack: true);
        }
    }
}