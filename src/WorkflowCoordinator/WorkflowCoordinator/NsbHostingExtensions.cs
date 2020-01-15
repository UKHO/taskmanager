using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NServiceBus;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.Messages;

namespace WorkflowCoordinator
{
    public static class NsbHostingExtensions
    {
        /// <summary>
        /// <para>Replaces <see cref="Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.RunAsync" /> to run an application
        /// and return a Task that only completes when the token is triggered or shutdown is triggered.</para>
        /// <para>Sends a <see cref="StartAssessmentPollingCommand" /> after the endpoint starts.</para>
        /// </summary>
        /// <param name="host">The <see cref="T:Microsoft.Extensions.Hosting.IHost" /> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
        public static async Task WorkflowCoordinatorRunAsync(this IHost host, CancellationToken token = default(CancellationToken))
        {
            try
            {
                await host.StartAsync(token);


                var startupOptions = host.Services.GetRequiredService<IOptions<StartupConfig>>();
                var nsbContext = host.Services.GetRequiredService<IMessageSession>();

                await nsbContext.SendLocal(new StartAssessmentPollingCommand(startupOptions.Value.AssessmentPollingSagaCorrelationGuid)).ConfigureAwait(false);

                await host.WaitForShutdownAsync(token);
            }
            finally
            {
                if (host is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else
                    host.Dispose();
            }
        }
    }
}