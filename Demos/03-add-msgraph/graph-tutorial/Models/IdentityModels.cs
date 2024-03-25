using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace graph_tutorial.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public DateTime DateOfJoining { get; set; }
        public string CanvasAPI { get; set; }
        public string InstructorProgram { get; set; }
        public string Tenant { get; set; }
        public string Subject { get; set; }
        public ICollection<Log> Logs { get; set; }
        public ICollection<CourseFile> CourseFiles { get; set; }


        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("Connection")
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Properties<DateTime>().Configure(c => c.HasColumnType("datetime2"));
        }

        public virtual DbSet<Log> Logs { get; set; }
        public virtual DbSet<CourseFile> CourseFiles { get; set; }

        public virtual DbSet<CachedUser> CachedUsers { get; set; }
        public IEnumerable ApplicationUsers { get; internal set; }


    }
}