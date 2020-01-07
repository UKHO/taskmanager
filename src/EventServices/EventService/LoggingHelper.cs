using System;
using System.Collections.ObjectModel;
using System.Data;
using Common.Helpers;
using EventService.Config;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace EventService
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
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn
                        {ColumnName = "UserFullName", DataType = SqlDbType.NVarChar, DataLength = 255},
                    new SqlColumn
                        {ColumnName = "EventName", DataType = SqlDbType.NVarChar, DataLength = 255},
                    new SqlColumn
                        {ColumnName = "EventBody", DataType = SqlDbType.NVarChar, DataLength = 4000},
                    new SqlColumn
                        {ColumnName = "CorrelationId", DataType = SqlDbType.UniqueIdentifier}
                }
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.MSSqlServer(loggingConnectionString,
                    "LoggingEventServices",
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