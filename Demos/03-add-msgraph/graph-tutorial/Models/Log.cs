using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace graph_tutorial.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }
        public string Action { get; set; }
        [Required]
        public string ApplicationUserId
        {
            get; set;
        }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public DateTime Timestamp { get; set; }
    }
}