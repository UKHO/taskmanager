using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using WorkflowDatabase.EF;

namespace Common.Helpers
{
    public static class DatabasesHelpers
    {
        public static string
            BuildSqlConnectionString(bool isLocalDb, string dataSource, string initialCatalog = "", string userId = "", string password = "") =>
            new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = initialCatalog,
                IntegratedSecurity = isLocalDb,
                MultipleActiveResultSets = true,
                Encrypt = isLocalDb ? false : true,
                ConnectTimeout = 20,
                UserID = userId,
                Password = password
            }.ToString();

        public static string BuildOracleConnectionString(string dataSource, string userId = "", string password = "") =>
            new OracleConnectionStringBuilder
            {
                DataSource = dataSource,
                UserID = userId,
                Password = password
            }.ToString();



        public static void ClearWorkflowDbTables(WorkflowDbContext workflowDbContext)
        {

            workflowDbContext.Database.ExecuteSqlCommand("delete from [LinkedDocument]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [Comment]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [AssessmentData]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [DbAssessmentReviewData]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [SourceDocumentStatus]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [WorkflowInstance]");
        }
    }
}