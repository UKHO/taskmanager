using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Portal.Hubs;

namespace Portal.HostedServices
{
    public class SqlListenerService : IHostedService
    {
        private readonly string _connectionString;
        private readonly IHubContext<TasksHub> _tasksHub;
        private readonly ILogger<SqlListenerService> _logger;

        public SqlListenerService(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var startupConfig = scope.ServiceProvider.GetRequiredService<IOptions<StartupConfig>>().Value;
            var listenerSecretsConfig = scope.ServiceProvider.GetRequiredService<IOptions<SqlListenerSecretsConfig>>().Value;
            _tasksHub = scope.ServiceProvider.GetRequiredService<IHubContext<TasksHub>>();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<SqlListenerService>>();

            var isLocalDevelopment = ConfigHelpers.IsLocalDevelopment;
            _connectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDevelopment,
                isLocalDevelopment ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName, listenerSecretsConfig.SqlAccountName, listenerSecretsConfig.SqlAccountPassword);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SqlDependency.Stop(_connectionString);
            SqlDependency.Start(_connectionString);

            StartListeningNewWorkflowInstance();

            return Task.CompletedTask;
        }

        private void StartListeningNewWorkflowInstance()
        {
            using var cn = new SqlConnection(_connectionString);
            using var cmd = cn.CreateCommand();

            cmd.CommandType = CommandType.Text;
            cmd.CommandText =
                @"select " +
                "[ProcessId]" +
                "from [dbo].[WorkflowInstance]" +
                "where [ActivityName] = 'Review'" +
                "and [Status] = 'Started'";

            cmd.Notification = null;
            var reviewTaskDepdency = new SqlDependency(cmd);
            reviewTaskDepdency.OnChange += WorkflowInstanceOnChange;

            cn.Open();
            cmd.ExecuteReader();
        }

        private void WorkflowInstanceOnChange(object sender, SqlNotificationEventArgs e)
        {
            try
            {
                if (e.Info == SqlNotificationInfo.Insert)
                {
                    _tasksHub.Clients.All.SendAsync("newReviewTask");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending new review task message through SignalR hub to all clients", ex);
            }
            finally
            {
                // Needs to be restarted
                StartListeningNewWorkflowInstance();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            SqlDependency.Stop(_connectionString);
            return Task.CompletedTask;
        }
    }
}
