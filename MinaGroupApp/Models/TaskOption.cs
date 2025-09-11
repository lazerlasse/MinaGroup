using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaGroupApp.Models
{
    public partial class TaskOption
    {
        public int TaskOptionId { get; set; }
        public string TaskName { get; set; } = string.Empty;

        public bool IsSelected { get; set; } = false;
    }
}
