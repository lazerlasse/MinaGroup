using MinaGroupApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.DataTransferObjects
{
    public class SelfEvaluationRequestDto
    {
        public bool IsSick { get; set; } = false;

        public TimeSpan ArrivalTime { get; set; } = TimeSpan.Zero;
        public TimeSpan DepartureTime { get; set; } = TimeSpan.Zero;
        public TimeSpan TotalHours { get; set; } = TimeSpan.Zero;
        public bool HadBreak { get; set; } = false;
        public TimeSpan BreakDuration { get; set; } = TimeSpan.Zero;

        public string ArrivalStatus { get; set; } = string.Empty;
        public List<TaskOptionDto> SelectedTasks { get; set; } = [];
        public string Collaboration { get; set; } = string.Empty;
        public string Assistance { get; set; } = string.Empty;

        public string Aid { get; set; } = string.Empty;
        public string AidDescription { get; set; } = string.Empty;
        public bool HadDiscomfort { get; set; } = false;
        public string DiscomfortDescription { get; set; } = string.Empty;

        public string CommentFromUser { get; set; } = string.Empty;
        public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;
    }
}
