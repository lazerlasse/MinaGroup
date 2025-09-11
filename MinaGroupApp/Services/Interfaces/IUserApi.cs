using MinaGroupApp.DataTransferObjects;
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
        [Headers("Authorization: Bearer")]
        [Get("/api/User/profile")]
        Task<UserProfileResponseDto> GetProfileAsync();
    }
}
