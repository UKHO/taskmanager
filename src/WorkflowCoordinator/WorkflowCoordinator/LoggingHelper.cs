using System;
using Common.Helpers;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator
{
    public class LoggingHelper
    {
        public static void SetupLogging(bool isLocalDb, StartupLoggingConfig startupLoggingConfig, StartupSecretsConfig startupSecretsConfig)
        {
            var loggingConnectionString = DatabasesHelpers.BuildSqlConnectionString(
                isLocalDb,
                isLocalDb ? startupLoggingConfig.LocalDbServer : startupLoggingConfig.WorkflowDbServer,
                isLocalDb ? startupLoggingConfig.LocalDbName : startupLoggingConfig.WorkflowDbName,
                startupSecretsConfig.SqlLoggingUsername, startupSecretsConfig.SqlLoggingPassword
            );

            Enum.TryParse(startupLoggingConfig.Level, out LogEventLevel logLevel);

            var columnOptions = new ColumnOptions
            {
                //AdditionalColumns = new Collection<SqlColumn>
                //{
                //    new SqlColumn
                //        {ColumnName = "UserFullName", DataType = SqlDbType.NVarChar, DataLength = 255},
                //    new SqlColumn
                //        {ColumnName = "EventName", DataType = SqlDbType.NVarChar, DataLength = 255},
                //    new SqlColumn
                //        {ColumnName = "EventBody", DataType = SqlDbType.NVarChar, DataLength = 4000},
                //    new SqlColumn
                //        {ColumnName = "CorrelationId", DataType = SqlDbType.UniqueIdentifier}
                //}
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.MSSqlServer(loggingConnectionString,
                    "LoggingWorkflowCoordinator",
                    null, //default
                    LogEventLevel.Verbose, //default
                    50, //default
                    null, //default
                    null, //default
                    true,
                    columnOptions)
                .CreateLogger();
        }
    }
}