using MinaGroupApp.DataTransferObjects;
using MinaGroupApp.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Services.Interfaces
{
    public interface ISelfEvaluationApi
    {
        [Post("/api/Selfevaluation/submit-evaluation")]
        Task SubmitEvaluationAsync([Body] SelfEvaluationRequestDto dto);
        
        [Get("/api/Selfevaluation/get-task-options")]
        Task<List<TaskOptionDto>> GetAvailableTasksAsync();

        [Get("/api/Selfevaluation/get-users-evaluationlist")]
        Task<List<SelfEvaluationResponseDto>> GetSelfEvaluationsListAsync();
    }
}
