using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Sql;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class SqlDatabaseOnboardingClient : IDataSourceOnboardingClient
{
    private readonly string resourceId;
    public readonly ResourceIdentifier resource;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal SqlDatabaseOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
    {
        if (resourceId.Split(separator: "/").Length != 11)
        {
            throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: nameof(resourceId));
        }
        this.resourceId = resourceId;
        this.resource = new ResourceIdentifier(resourceId: resourceId);
        this.purviewAutomationClient = client;
        this.logger = logger;
    }

    private async Task<GenericResource> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get resource
        var resource = await armClient.GetGenericResource(id: new ResourceIdentifier(resourceId: this.resourceId)).GetAsync();

        return resource.Value;
    }

    private async Task<SqlServerResource> GetParentResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get sql server
        var subscription = await armClient.GetSubscriptions().GetAsync(subscriptionId: this.resource.SubscriptionId);
        var sqlServers = subscription.Value.GetSqlServersAsync();
        await foreach (var sqlServer in sqlServers)
        {
            if (sqlServer.HasData && sqlServer.Data.Name.Equals(this.resource.Parent.Name))
            {
                return sqlServer;
            }
        }
        return null;
    }

    public async Task AddDataSourceAsync()
    {
        logger.LogInformation("Azure SQL Database does not have to be onboarded. Only the Azure SQL Server must be onboarded.");
    }

    public async Task<string> AddScanningManagedPrivateEndpointsAsync()
    {
        logger.LogInformation("Managed Private Endpoints are not required for Azure SQL Database. Only required for the Azure SQL Server.");
        return null;
    }

    public async Task AddScanAsync(bool triggerScan, string managedIntegrationRuntimeName)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveDataSourceAsync()
    {
        // Remove data source
        await this.purviewAutomationClient.RemoveDataSourceAsync(dataSourceName: this.resource.Name);
    }

    public async Task OnboardDataSourceAsync(bool useManagedPrivateEndpoints, bool setupScan, bool triggerScan)
    {
        await this.AddDataSourceAsync();

        string managedIntegrationRuntimeName = string.Empty;
        if (useManagedPrivateEndpoints)
        {
            managedIntegrationRuntimeName = await this.AddScanningManagedPrivateEndpointsAsync();
        }
        if (setupScan)
        {
            // await this.AddScanAsync(triggerScan: triggerScan, managedIntegrationRuntimeName: managedIntegrationRuntimeName);
        }
    }
}
