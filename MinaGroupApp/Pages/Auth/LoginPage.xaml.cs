using MinaGroupApp.ViewModels;
using MinaGroupApp.ViewModels.Auth;
using System.Threading.Tasks;

namespace MinaGroupApp.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
	}
}