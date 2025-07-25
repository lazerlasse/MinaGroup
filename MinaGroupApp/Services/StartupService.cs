using MinaGroupApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services
{
    public class StartupService
    {
        private readonly IAuthService _authService;

        public StartupService(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<bool> TryLoginAsync()
        {
            return await _authService.TryAutoLoginAsync();
        }
    }
}
