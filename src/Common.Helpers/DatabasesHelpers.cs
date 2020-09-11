using DbUpdateWorkflowDatabase.EF;
using Microsoft.EntityFrameworkCore;
using NCNEWorkflowDatabase.EF;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data.SqlClient;
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
            workflowDbContext.Database.ExecuteSqlRaw("delete from [LinkedDocument]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [Comments]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [AssessmentData]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [DbAssessmentReviewData]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [DbAssessmentAssessData]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [DbAssessmentVerifyData]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [PrimaryDocumentStatus]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [OnHold]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [TaskNote]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [DatabaseDocumentStatus]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [ProductAction]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [ProductActionType]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [AssignedTaskType]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [DataImpact]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [HpdUser]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [HpdUsage]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [DbAssessmentAssignTask]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [CarisProjectDetails]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [WorkflowInstance]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [AdUsers]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [CachedHpdWorkspace]");
            workflowDbContext.Database.ExecuteSqlRaw("delete from [OpenAssessmentsQueue]");

            ReSeedWorkflowDbTables(workflowDbContext);
            workflowDbContext.SaveChanges();
        }

        private static void ReSeedWorkflowDbTables(WorkflowDbContext dbContext)
        {
            dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT('AdUsers', RESEED, 0)");
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
            dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT('TaskInfo', RESEED, 0)");
            dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT('AdUser', RESEED, 0)");
            dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT('TaskRole', RESEED, 0)");
        }

        public static void ClearDbUpdateWorkflowDbTables(DbUpdateWorkflowDbContext dbContext,
            bool reseedIdentity = true)
        {
            dbContext.TaskStageComment.RemoveRange(dbContext.TaskStageComment);
            dbContext.TaskComment.RemoveRange(dbContext.TaskComment);
            dbContext.TaskRole.RemoveRange(dbContext.TaskRole);
            dbContext.TaskStage.RemoveRange(dbContext.TaskStage);

            //Enforce the deletion of taskStage before taskStageType
            dbContext.SaveChanges();

            dbContext.TaskNote.RemoveRange(dbContext.TaskNote);
            dbContext.CarisProjectDetails.RemoveRange(dbContext.CarisProjectDetails);
            dbContext.TaskInfo.RemoveRange(dbContext.TaskInfo);
            dbContext.TaskStageType.RemoveRange(dbContext.TaskStageType);
            dbContext.ChartingArea.RemoveRange(dbContext.ChartingArea);
            dbContext.UpdateType.RemoveRange(dbContext.UpdateType);
            dbContext.ProductAction.RemoveRange(dbContext.ProductAction);

            dbContext.HpdUser.RemoveRange(dbContext.HpdUser);
            dbContext.AdUser.RemoveRange(dbContext.AdUser);

            if (reseedIdentity) ReseedDbUpdateWorkflowDbTables(dbContext);

            dbContext.SaveChanges();
        }

        private static void ReseedDbUpdateWorkflowDbTables(DbUpdateWorkflowDbContext dbContext)
        {
            dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT('TaskInfo', RESEED, 0)");
            dbContext.Database.ExecuteSqlRaw("DBCC CHECKIDENT('AdUser', RESEED, 0)");
        }

        public static WorkflowDbContext GetInMemoryWorkflowDbContext()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;
            return new WorkflowDbContext(dbContextOptions);
        }
    }
}