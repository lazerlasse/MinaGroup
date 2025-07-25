using MinaGroupApp.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services.Interfaces
{
    public interface IUserApi
    {
        [Get("/api/User/profile")]
        Task<UserProfileResponseDto> GetProfileAsync();
    }
}
