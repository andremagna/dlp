using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.dtos.Project
{
    public class CreateProjectRequesDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string ProjectPlace { get; set; } = string.Empty;
        //public string ProjectYear { get; set; } = string.Empty;
         public DateTime ProjectYear { get; set; } = new DateTime();
        public string ProjectParentFilter { get; set; } = string.Empty;
        public string ProjectChildFilter { get; set; } = string.Empty;
        public string ProjectCover { get; set; } = string.Empty;
        public List<string> ProjectImages { get; set; } = new List<string>();
    }
}