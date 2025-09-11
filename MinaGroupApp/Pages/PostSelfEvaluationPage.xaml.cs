using MinaGroupApp.ViewModels;

namespace MinaGroupApp.Pages;

public partial class PostSelfEvaluationPage : ContentPage
{
	public PostSelfEvaluationPage(PostSelfEvaluationViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;

        Loaded += async (_, _) => await viewModel.LoadTasksAsync();
    }
}