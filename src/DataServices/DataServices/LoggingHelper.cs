using System;
using System.Collections.ObjectModel;
using System.Data;
using Common.Helpers;
using DataServices.Config;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.MSSqlServer.Sinks.MSSqlServer.Options;

namespace DataServices
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
                        {ColumnName = "ApiResource", DataType = SqlDbType.NVarChar, DataLength = 255},
                    new SqlColumn
                        {ColumnName = "SdocId", DataType = SqlDbType.Int}
                }
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.MSSqlServer(
                    connectionString: loggingConnectionString,
                    sinkOptions: new SinkOptions
                    {
                        TableName = "LoggingDataServices",
                        AutoCreateSqlTable = true

                    },
                    columnOptions: columnOptions)
                .CreateLogger();
        }
    }
}