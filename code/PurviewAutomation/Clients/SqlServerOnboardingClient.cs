using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class SqlServerOnboardingClient : IDataSourceOnboardingClient
{
    private readonly string resourceId;
    public readonly ResourceIdentifier resource;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal SqlServerOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
    {
        if (resourceId.Split(separator: "/").Length != 9)
        {
            throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: nameof(resourceId));
        }
        this.resourceId = resourceId;
        this.resource = new ResourceIdentifier(resourceId: resourceId);
        this.purviewAutomationClient = client;
        this.logger = logger;
    }

    private async Task<SqlServerResource> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get sql server
        var subscription = await armClient.GetSubscriptions().GetAsync(subscriptionId: this.resource.SubscriptionId);
        var resourceGroup = await subscription.Value.GetResourceGroupAsync(resourceGroupName: this.resource.ResourceGroupName);
        var sqlServer = await resourceGroup.Value.GetSqlServerAsync(serverName: this.resource.Name);

        return sqlServer.Value;
    }

    public async Task AddDataSourceAsync()
    {
        // Get resource
        var sqlServer = await this.GetResourceAsync();

        // Create data source
        var dataSource = new
        {
            name = this.resource.Name,
            kind = "AzureSqlDatabase",
            properties = new
            {
                resourceId = this.resourceId,
                subscriptionId = this.resource.SubscriptionId,
                resourceGroup = this.resource.ResourceGroupName,
                resourceName = this.resource.Name,
                serverEndpoint = sqlServer.Data.FullyQualifiedDomainName,
                location = sqlServer.Data.Location.ToString(),
                collection = new
                {
                    referenceName = this.resource.ResourceGroupName,
                    type = "CollectionReference"
                }
            }
        };

        // Add data source
        await this.purviewAutomationClient.AddDataSourceAsync(subscriptionId: this.resource.SubscriptionId, resourceGroupName: this.resource.ResourceGroupName, dataSourceName: this.resource.Name, dataSource: dataSource);
    }

    public async Task<string> AddScanningManagedPrivateEndpointsAsync()
    {
        // Create managed private endpoints
        return await this.purviewAutomationClient.CreateManagedPrivateEndpointAsync(name: this.resource.Name, groupId: "sqlServer", resourceId: this.resourceId);
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

        var managedIntegrationRuntimeName = string.Empty;
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
