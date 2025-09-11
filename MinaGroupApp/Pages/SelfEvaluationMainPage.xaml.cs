using MinaGroupApp.ViewModels;

namespace MinaGroupApp.Pages;

public partial class SelfEvaluationMainPage : ContentPage
{
	public SelfEvaluationMainPage(SelfEvaluationMainPageViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;

        Loaded += async (_, _) => await viewModel.GetSelfEvaluationsAsync();
    }
}