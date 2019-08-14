using System.Data.SqlClient;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;

namespace WorkflowDatabase.EF
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options, string azureAccessToken = "")
            : base(options)
        {
            if (!string.IsNullOrEmpty(azureAccessToken))
            {
                var conn = Database.GetDbConnection() as SqlConnection;
                conn.AccessToken = (new AzureServiceTokenProvider()).GetAccessTokenAsync("https://database.windows.net/").Result;
            }
        }

        public DbSet<Models.AssessmentData> AssessmentData { get; set; }
        public DbSet<Models.Comment> Comments { get; set; }
        public DbSet<Models.DbAssessmentReviewData> DbAssessmentReviewData { get; set; }
        public DbSet<Models.Process> Processes { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }
    }
}
