using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class CosmosOnboardingClient : IDataSourceOnboardingClient
{
    private readonly string resourceId;
    public readonly ResourceIdentifier resource;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal CosmosOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
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

    private async Task<CosmosDBAccountResource> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get database
        var subscription = await armClient.GetSubscriptions().GetAsync(subscriptionId: this.resource.SubscriptionId);
        var resourceGroup = await subscription.Value.GetResourceGroupAsync(resourceGroupName: this.resource.ResourceGroupName);
        var cosmos = await resourceGroup.Value.GetCosmosDBAccountAsync(accountName: this.resource.Name);

        return cosmos.Value;
    }

    public async Task AddDataSourceAsync()
    {
        // Get resource
        var cosmos = await this.GetResourceAsync();

        // Create data source
        var dataSource = new
        {
            name = this.resource.Name,
            kind = "AzureCosmosDb",
            properties = new
            {
                resourceId = this.resourceId,
                subscriptionId = this.resource.SubscriptionId,
                resourceGroup = this.resource.ResourceGroupName,
                resourceName = this.resource.Name,
                accountUri = $"https://{this.resource.Name}.documents.azure.com:443/",
                location = cosmos.Data.Location.ToString(),
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
        return await this.purviewAutomationClient.CreateManagedPrivateEndpointAsync(name: this.resource.Name, groupId: "sql", resourceId: this.resourceId);
    }

    public async Task AddScanAsync(bool triggerScan, string managedIntegrationRuntimeName)
    {
        // Get resource
        var cosmos = await this.GetResourceAsync();

        // Get cosmos key
        var primaryKey = cosmos.GetKeys().Value.PrimaryMasterKey;

        // TODO: Store key in Key Vault
        // TODO: Create scan

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
            await this.AddScanAsync(triggerScan: triggerScan, managedIntegrationRuntimeName: managedIntegrationRuntimeName);
        }
    }
}
