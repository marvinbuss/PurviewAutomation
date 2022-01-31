using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class PostgreSqlServerOnboardingClient : IDataSourceOnboardingClient
{
    private readonly string resourceId;
    public readonly ResourceIdentifier resource;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal PostgreSqlServerOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
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

    private async Task<Azure.Response<GenericResource>> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get resource
        return await armClient.GetGenericResource(id: new ResourceIdentifier(resourceId: this.resourceId)).GetAsync();
    }

    public async Task AddDataSourceAsync()
    {
        // Get resource
        var postgreSqlServer = await this.GetResourceAsync();

        // Create data source
        var dataSource = new
        {
            name = this.resource.Name,
            kind = "AzurePostgreSql",
            properties = new
            {
                resourceId = this.resourceId,
                subscriptionId = this.resource.SubscriptionId,
                resourceGroup = this.resource.ResourceGroupName,
                resourceName = this.resource.Name,
                serverEndpoint = $"{postgreSqlServer.Value.Data.Name}.postgres.database.azure.com",
                location = postgreSqlServer.Value.Data.Location.ToString(),
                port = "5432",
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

    public async Task AddScanAsync(bool triggerScan)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveDataSourceAsync()
    {
        // Remove data source
        await this.purviewAutomationClient.RemoveDataSourceAsync(dataSourceName: this.resource.Name);
    }

    public async Task OnboardDataSourceAsync(bool setupScan, bool triggerScan)
    {
        await this.AddDataSourceAsync();

        if (setupScan)
        {
            // await this.AddScanAsync(triggerScan: triggerScan);
        }
    }
}
