using System;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NCNEWorkflowDatabase.EF;
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
            workflowDbContext.Database.ExecuteSqlCommand("delete from [DbAssessmentAssessData]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [DbAssessmentVerifyData]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [PrimaryDocumentStatus]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [OnHold]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [TaskNote]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [DatabaseDocumentStatus]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [ProductAction]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [ProductActionType]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [AssignedTaskType]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [DataImpact]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [AdUser]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [HpdUsage]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [DbAssessmentAssignTask]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [CarisProjectDetails]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [WorkflowInstance]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [HpdUser]");
            workflowDbContext.Database.ExecuteSqlCommand("delete from [CachedHpdWorkspace]");
        }



        public static void ReCreateLocalDb(string localDbServer, string dbName, string connectionString, bool isLocalDebugging)
        {
            var connectionStringObject = new SqlConnectionStringBuilder(connectionString);
            if (!isLocalDebugging || !connectionStringObject.DataSource.Equals(localDbServer))
            {
                throw new InvalidOperationException($@"{nameof(ReCreateLocalDb)} should only be called when executing in local development environment.");
            }

            var sanitisedDbName = dbName.Replace("'", "''");

            var commandText = "USE master " +
                              $"IF NOT EXISTS(select * from sys.databases where name='{sanitisedDbName}') " +
                              "BEGIN " +
                              $"CREATE DATABASE [{sanitisedDbName}] " +
                              "END";

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(commandText, connection);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }

            }
        }

        public static void ClearNcneWorkflowDbTables(NcneWorkflowDbContext dbContext, bool reseedIdentity = true)
        {
            dbContext.TaskStageComment.RemoveRange(dbContext.TaskStageComment);
            dbContext.TaskComment.RemoveRange(dbContext.TaskComment);
            dbContext.TaskRole.RemoveRange(dbContext.TaskRole);
            dbContext.TaskStage.RemoveRange(dbContext.TaskStage);

            //Enforce the deletion of TaskStage before TaskStageType
            dbContext.SaveChanges();

            dbContext.TaskNote.RemoveRange(dbContext.TaskNote);
            dbContext.CarisProjectDetails.RemoveRange(dbContext.CarisProjectDetails);
            dbContext.TaskInfo.RemoveRange(dbContext.TaskInfo);
            dbContext.TaskStageType.RemoveRange(dbContext.TaskStageType);
            dbContext.ChartType.RemoveRange(dbContext.ChartType);
            dbContext.WorkflowType.RemoveRange(dbContext.WorkflowType);

            dbContext.HpdUser.RemoveRange(dbContext.HpdUser);
            dbContext.AdUser.RemoveRange(dbContext.AdUser);

            if (reseedIdentity) ReSeedNcneWorkflowDbTables(dbContext);

            dbContext.SaveChanges();
        }

        private static void ReSeedNcneWorkflowDbTables(NcneWorkflowDbContext dbContext)
        {
            dbContext.Database.ExecuteSqlCommand("DBCC CHECKIDENT('TaskInfo', RESEED, 0)");
        }
    }
}