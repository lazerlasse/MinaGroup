using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinaGroupApp.DataTransferObjects;
using MinaGroupApp.Models;
using MinaGroupApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.ViewModels
{
    public partial class PostSelfEvaluationViewModel : BaseViewModel
    {
        private readonly ISelfEvaluationApi _evaluationApi;
        private readonly INavigationService _navigationService;

        public SelfEvaluationFormModel Form { get; set; } = new();

        public PostSelfEvaluationViewModel(ISelfEvaluationApi evaluationApi, INavigationService navigationService)
        {
            _evaluationApi = evaluationApi;
            _navigationService = navigationService;

            Title = "Daglig Selvevaluering";
        }

        [RelayCommand]
        public async Task SubmitAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Make list of selected tasks.
                var selectedTask = new List<TaskOptionDto>();
                foreach (var task in Form.AvailableTasks)
                {
                    if (task.IsSelected)
                    {
                        selectedTask.Add(new TaskOptionDto
                        {
                            TaskOptionId = task.TaskOptionId,
                            TaskName = task.TaskName
                        });
                    }
                }

                var dto = new SelfEvaluationRequestDto
                {
                    ArrivalTime = Form.ArrivalTime,
                    DepartureTime = Form.DepartureTime,
                    TotalHours = Form.TotalHours,
                    HadBreak = Form.HadBreak,
                    BreakDuration = Form.BreakDuration,
                    ArrivalStatus = Form.ArrivalStatus,
                    SelectedTasks = selectedTask,
                    Collaboration = Form.Collaboration,
                    Assistance = Form.Assistance,
                    Aid = Form.Aid,
                    AidDescription = Form.AidDescription,
                    HadDiscomfort = Form.HadDiscomfort,
                    DiscomfortDescription = Form.DiscomfortDescription,
                    CommentFromUser = Form.CommentFromUser,
                    IsSick = Form.IsSick
                };

                await _evaluationApi.SubmitEvaluationAsync(dto);
                
                await Shell.Current.DisplayAlert("Succes", "Selvevalueringen blev sendt.", "OK");

                await _navigationService.NavigateToAsync("///FrontPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fejl", $"Kunne ikke sende: {ex.Message} - Prøv venligst igen!", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadTasksAsync()
        {
            IsBusy = true;

            try
            {
                var tasks = await _evaluationApi.GetAvailableTasksAsync();
                MainThread.BeginInvokeOnMainThread((Action)(() =>
                {
                    foreach (var task in tasks)
                    {
                        Form.AvailableTasks.Add(new()
                        {
                            TaskOptionId = task.TaskOptionId,
                            TaskName = task.TaskName
                        });
                    }
                }));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fejl", "Kunne ikke hente nødvendige data: " + ex.Message + " - Prøv venligst igen!", "OK");

                await _navigationService.NavigateToAsync("FrontPage");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}