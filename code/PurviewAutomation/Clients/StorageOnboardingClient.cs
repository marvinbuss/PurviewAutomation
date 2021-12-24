using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.Logging;

namespace PurviewAutomation.Clients;

internal class StorageOnboardingClient : IDataSourceOnboardingClient
{
    private readonly string resourceId;
    public readonly ResourceIdentifier resource;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal StorageOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
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

    private async Task<Azure.Response<StorageAccount>> GetStorageAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get storage
        var resourceGroup = armClient.GetResourceGroup(id: new ResourceIdentifier(resourceId: $"/subscriptions/{this.resource.SubscriptionId}/resourceGroups/{this.resource.ResourceGroupName}"));
        return await resourceGroup.GetStorageAccounts().GetAsync(accountName: this.resource.Name);
    }

    public async Task AddDataSourceAsync()
    {
        // Get resource
        var storage = await this.GetStorageAsync();

        // Create data source
        var dataSource = new
        {
            name = this.resource.Name,
            kind = storage.Value.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2" : "AzureStorage",
            properties = new
            {
                resourceId = resourceId,
                subscriptionId = this.resource.SubscriptionId,
                resourceGroup = this.resource.ResourceGroupName,
                resourceName = this.resource.Name,
                endpoint = storage.Value.Data.IsHnsEnabled.Equals(true) ? $"https://{this.resource.Name}.dfs.core.windows.net/" : $"https://{this.resource.Name}.blob.core.windows.net/",
                location = storage.Value.Data.Location.ToString(),
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

    public async Task AddScanAsync(bool triggerScan = true)
    {
        // Get resource
        var storage = await this.GetStorageAsync();

        // Create scan
        var scanName = "default";
        var scan = new
        {
            name = scanName,
            kind = storage.Value.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2Msi" : "AzureStorageMsi",
            properties = new
            {
                scanRulesetName = storage.Value.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2" : "AzureStorage",
                scanRulesetType = "System",
                collection = new
                {
                    referenceName = this.resource.ResourceGroupName,
                    type = "CollectionReference"
                }
            }
        };

        // Create trigger
        var triggerName = "default";
        var trigger = new
        {
            name = triggerName,
            properties = new
            {
                scanLevel = "Incremental",
                recurrence = new
                {
                    frequency = "Week",
                    interval = 1,
                    startTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssZ"),
                    timezone = "UTC",
                    schedule = new
                    {
                        hours = new int[] { 3 },
                        minutes = new int[] { 0 },
                        weekDays = new string[] { "Sunday" }
                    }
                }
            }
        };

        // Create Filter
        var filter = new
        {
            properties = new
            {
                excludeUriPrefixes = new string[] { },
                includeUriPrefixes = new string[] { storage.Value.Data.IsHnsEnabled.Equals(true) ? $"https://{this.resource.Name}.dfs.core.windows.net/" : $"https://{this.resource.Name}.blob.core.windows.net" }
            }
        };

        // Create scan
        if (triggerScan)
        {
            await this.purviewAutomationClient.AddScanAsync(dataSourceName: this.resource.Name, scan: scan, scanName: scanName, runScan: true, trigger: trigger, filter: filter);
        }
    }

    public async Task RemoveDataSourceAsync()
    {
        // Remove data source
        await this.purviewAutomationClient.RemoveDataSourceAsync(dataSourceName: this.resource.Name);
    }

    public async Task OnboardDataSourceAsync(bool setupScan = true, bool triggerScan = true)
    {
        await this.AddDataSourceAsync();

        if (setupScan)
        {
            await this.AddScanAsync(triggerScan: triggerScan);
        }
    }
}
