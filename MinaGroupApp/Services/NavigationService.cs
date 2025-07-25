using MinaGroupApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services
{
    public class NavigationService : INavigationService
    {
        public async Task NavigateToAsync(string route, bool clearStack = false)
        {
            if (clearStack)
                await Shell.Current.GoToAsync(route, true);
            else
                await Shell.Current.GoToAsync(route);
        }
    }
}
