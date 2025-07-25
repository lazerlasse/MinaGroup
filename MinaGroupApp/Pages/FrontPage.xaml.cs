using MinaGroupApp.ViewModels;

namespace MinaGroupApp.Pages;

public partial class FrontPage : ContentPage
{
    private readonly FrontPageViewModel _viewModel;

    public FrontPage(FrontPageViewModel viewModel)
	{
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        Loaded += async (_, _) => await _viewModel.InitAsync();
    }
}