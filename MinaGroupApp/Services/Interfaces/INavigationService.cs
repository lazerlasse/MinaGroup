using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services.Interfaces
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route, bool clearStack = false);
    }
}
