using System;
using System.Collections.ObjectModel;
using System.Data;
using Common.Helpers;
using DataServices.Config;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace DataServices
{
    public class LoggingHelper
    {
        public static void SetupLogging(bool isLocalDb, StartupLoggingConfig startupLoggingConfig)
        {
            var loggingConnectionString = DatabasesHelpers.BuildSqlConnectionString(
                isLocalDb,
                isLocalDb ? startupLoggingConfig.LocalDbServer : startupLoggingConfig.WorkflowDbServer,
                isLocalDb ? startupLoggingConfig.LocalDbName : startupLoggingConfig.WorkflowDbName
            );

            Enum.TryParse(startupLoggingConfig.Level, out LogEventLevel logLevel);

            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn
                        {ColumnName = "UserName", DataType = SqlDbType.NVarChar, DataLength = 255},
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
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.MSSqlServer(loggingConnectionString,
                    "LoggingDataServices",
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