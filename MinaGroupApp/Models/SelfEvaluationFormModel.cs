using CommunityToolkit.Mvvm.ComponentModel;
using MinaGroupApp.Models;
using System.Collections.ObjectModel;

public partial class SelfEvaluationFormModel : ObservableObject
{
    public SelfEvaluationFormModel()
    {
        YesNoOptions = new(["Ja", "Nej"]);
        ArrivalOptions = new(["Intet valgt" , "Til tiden", "Forsent", "Aftalt forsinkelse"]);
        CollaborationOptions = new(["Intet valgt", "Godt", "Okay", "Dårligt"]);
        AssistanceOptions = new(["Intet valgt", "Klarer det selv", "Lidt hjælp", "Meget hjælp"]);
        AidOptions = new(["Nej", "Ja – hvilke?", "Har brug for noget – hvad?"]);
    }

    // === Valglister ===
    [ObservableProperty] private ObservableCollection<string> yesNoOptions;
    [ObservableProperty] private ObservableCollection<string> arrivalOptions;
    [ObservableProperty] private ObservableCollection<string> collaborationOptions;
    [ObservableProperty] private ObservableCollection<string> assistanceOptions;
    [ObservableProperty] private ObservableCollection<string> aidOptions;

    [ObservableProperty] private ObservableCollection<TaskOption> availableTasks = [];

    // === Ja/nej svar ===
    [ObservableProperty] private string isSickAnswer = "Nej";
    [ObservableProperty] private string hadBreakAnswer = "Nej";
    [ObservableProperty] private string hadDiscomfortAnswer = "Nej";

    public bool IsSick => IsSickAnswer == "Ja";
    public bool ShowMainForm => IsSickAnswer == "Nej";
    public bool ShowBreakDuration => HadBreakAnswer == "Ja";
    public bool HadBreak => HadBreakAnswer == "Ja";
    public bool ShowDiscomfortField => HadDiscomfortAnswer == "Ja";
    public bool HadDiscomfort => HadDiscomfortAnswer == "Ja";

    partial void OnIsSickAnswerChanged(string value)
    {
        OnPropertyChanged(nameof(IsSick));
        OnPropertyChanged(nameof(ShowMainForm));
    }

    partial void OnHadBreakAnswerChanged(string value)
    {
        OnPropertyChanged(nameof(ShowBreakDuration));
        OnPropertyChanged(nameof(HadBreak));
        OnPropertyChanged(nameof(TotalHoursDisplay));
    }

    partial void OnHadDiscomfortAnswerChanged(string value)
    {
        OnPropertyChanged(nameof(ShowDiscomfortField));
        OnPropertyChanged(nameof(HadDiscomfort));
    }

    // === Tid ===
    [ObservableProperty] private TimeSpan arrivalTime = TimeSpan.Zero;
    [ObservableProperty] private TimeSpan departureTime = TimeSpan.Zero;
    [ObservableProperty] private TimeSpan breakDuration = TimeSpan.Zero;

    partial void OnArrivalTimeChanged(TimeSpan value) => OnPropertyChanged(nameof(TotalHoursDisplay));
    partial void OnDepartureTimeChanged(TimeSpan value) => OnPropertyChanged(nameof(TotalHoursDisplay));
    partial void OnBreakDurationChanged(TimeSpan value) => OnPropertyChanged(nameof(TotalHoursDisplay));

    public TimeSpan TotalHours => (DepartureTime - ArrivalTime) - (ShowBreakDuration ? BreakDuration : TimeSpan.Zero);
    public string TotalHoursDisplay => $"{(int)TotalHours.TotalHours:00}:{TotalHours.Minutes:00}";

    // === Øvrige svar ===
    [ObservableProperty] private string arrivalStatus = "Intet valgt";
    [ObservableProperty] private string collaboration = "Intet valgt";
    [ObservableProperty] private string assistance = "Intet valgt";
    [ObservableProperty] private string aid = "Nej";
    [ObservableProperty] private string aidDescription = string.Empty;
    public bool ShowAidField => Aid == "Ja – hvilke?" || Aid == "Har brug for noget – hvad?";
    partial void OnAidChanged(string value) => OnPropertyChanged(nameof(ShowAidField));
    [ObservableProperty] private string discomfortDescription = string.Empty;
    [ObservableProperty] private string commentFromUser = string.Empty;
}