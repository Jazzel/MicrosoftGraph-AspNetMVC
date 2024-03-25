using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace graph_tutorial.Models
{
    public class CourseFile
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Term { get; set; }
        public string Url { get; set; }
        public string CourseProgram { get; set; }
        public string CourseType { get; set; }
        public string CourseSection { get; set; }
        public string Status { get; set; }
        [Required]
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

    }
}