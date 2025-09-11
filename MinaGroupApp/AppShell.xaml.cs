using MinaGroupApp.Pages;

namespace MinaGroupApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(PostSelfEvaluationPage), typeof(PostSelfEvaluationPage));
        }
    }
}
