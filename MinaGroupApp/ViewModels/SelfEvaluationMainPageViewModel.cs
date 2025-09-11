using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using MinaGroupApp.DataTransferObjects;
using MinaGroupApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.ViewModels
{
    public partial class SelfEvaluationMainPageViewModel : BaseViewModel
    {
        private ISelfEvaluationApi _api;

        [ObservableProperty]
        private ObservableCollection<SelfEvaluationResponseDto> selfEvaluations = [];

        public SelfEvaluationMainPageViewModel(ISelfEvaluationApi api)
        {
            _api = api;

            Title = "Selvevaluerings Oversigt";
        }

        public async Task GetSelfEvaluationsAsync()
        {
            if(IsBusy)
                return;

            IsBusy = true;

            try
            {
                var evaluations = await _api.GetSelfEvaluationsListAsync();

                SelfEvaluations = evaluations.ToObservableCollection();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fejl", "Kunne ikke hente nødvendige data: " + ex.Message + " - Prøv venligst igen!", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
