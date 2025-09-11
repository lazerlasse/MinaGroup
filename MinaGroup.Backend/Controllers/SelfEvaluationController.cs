using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.DataTransferObjects;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class SelfEvaluationController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbContext;

        public SelfEvaluationController(UserManager<AppUser> userManager, AppDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        [HttpPost("submit-evaluation")]
        public async Task<IActionResult> Submit(SelfEvaluationDto dto)
        {
            // Check user is authorized or retur unauthorized response.
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("Bruger ikke fundet.");

            try
            {
                // Create new evaluationshema for saving data to db.
                var evaluation = new SelfEvaluation
                {
                    UserId = user.Id,
                    ArrivalTime = dto.ArrivalTime,
                    DepartureTime = dto.DepartureTime,
                    TotalHours = dto.TotalHours,
                    HadBreak = dto.HadBreak,
                    BreakDuration = dto.BreakDuration,
                    ArrivalStatus = dto.ArrivalStatus,
                    SelectedTask = [],
                    Collaboration = dto.Collaboration,
                    Assistance = dto.Assistance,
                    Aid = dto.Aid,
                    AidDescription = dto.AidDescription,
                    HadDiscomfort = dto.HadDiscomfort,
                    DiscomfortDescription = dto.DiscomfortDescription,
                    CommentFromUser = dto.CommentFromUser,
                    EvaluationDate = dto.EvaluationDate
                };

                // Load taskoptions from db.
                foreach (var task in dto.SelectedTasks)
                {
                    var taskOption = await _dbContext.TaskOptions.FindAsync(task.TaskOptionId);

                    if (taskOption != null)
                        evaluation.SelectedTask.Add(taskOption);
                }

                // Add to db context and save to db.
                _dbContext.SelfEvaluations.Add(evaluation);
                await _dbContext.SaveChangesAsync();

                // Return Ok.
                return Ok(new { message = "Skema indsendt", id = evaluation.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Fejl ved indsendelse af skema", error = ex.Message });
            }
        }

        // (Ekstra: Liste over brugerens skemaer)
        [HttpGet("get-users-evaluationlist")]
        public async Task<IActionResult> GetUsersEvaluationList()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                var evaluations = await _dbContext.SelfEvaluations
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.EvaluationDate)
                .ToListAsync();

                var responses = new List<SelfEvaluationResponseDto>();

                foreach (var evaluation in evaluations)
                {
                    responses.Add(new()
                    {
                        IsSick = evaluation.IsSick,
                        ArrivalTime = evaluation.ArrivalTime,
                        DepartureTime = evaluation.DepartureTime,
                        ArrivalStatus = evaluation.ArrivalStatus,
                        HadBreak = evaluation.HadBreak,
                        BreakDuration = evaluation.BreakDuration,
                        TotalHours = evaluation.TotalHours,
                        SelectedTasks = evaluation.SelectedTask.ToList(),
                        Assistance = evaluation.Assistance,
                        Collaboration = evaluation.Collaboration,
                        HadDiscomfort = evaluation.HadDiscomfort,
                        DiscomfortDescription = evaluation.DiscomfortDescription,
                        Aid = evaluation.Aid,
                        AidDescription = evaluation.AidDescription,
                        CommentFromUser = evaluation.CommentFromUser,
                        EvaluationDate = evaluation.EvaluationDate
                    });
                }

                return Ok(responses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Der opstod en fejl ved hentning af evalueringsskemaerne.", error = ex.Message });
            }
        }

        // GET: Get list of TaskOptions
        [HttpGet("get-task-options")]
        public async Task<IActionResult> GetTaskOptions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                var taskOptions = await _dbContext.TaskOptions.ToListAsync();

                return Ok(taskOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Der opatod en fejl ved hentning af opgaver.", error = ex.Message });
            }
        }
    }
}