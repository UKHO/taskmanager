using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;

namespace Common.Helpers
{
    public static class DatabasesHelpers
    {
        public static string
            BuildSqlConnectionString(bool isLocalDb, string dataSource, string initialCatalog = "") =>
            new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = initialCatalog,
                IntegratedSecurity = isLocalDb,
                Encrypt = isLocalDb ? false : true,
                ConnectTimeout = 20
            }.ToString();

        public static void ClearWorkflowDbTables(WorkflowDbContext workflowDbContext)
        {
            workflowDbContext.Database.ExecuteSqlCommand("delete from [Comment]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [AssessmentData]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [DbAssessmentReviewData]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [WorkflowInstance]");
        }
    }
}