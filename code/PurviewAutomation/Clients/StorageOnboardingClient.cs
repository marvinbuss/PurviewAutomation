using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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

    private async Task<StorageAccountResource> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get storage
        var subscription = await armClient.GetSubscriptions().GetAsync(subscriptionId: this.resource.SubscriptionId);
        var resourceGroup = await subscription.Value.GetResourceGroupAsync(resourceGroupName: this.resource.ResourceGroupName);
        var storage = await resourceGroup.Value.GetStorageAccountAsync(accountName: this.resource.Name);

        return storage.Value;
    }

    public async Task AddDataSourceAsync()
    {
        // Get resource
        var storage = await this.GetResourceAsync();

        // Create data source
        var dataSource = new
        {
            name = this.resource.Name,
            kind = storage.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2" : "AzureStorage",
            properties = new
            {
                resourceId = resourceId,
                subscriptionId = this.resource.SubscriptionId,
                resourceGroup = this.resource.ResourceGroupName,
                resourceName = this.resource.Name,
                endpoint = storage.Data.IsHnsEnabled.Equals(true) ? $"https://{this.resource.Name}.dfs.core.windows.net/" : $"https://{this.resource.Name}.blob.core.windows.net/",
                location = storage.Data.Location.ToString(),
                // dataUseGovernance = "Enabled",
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
        // Get resource
        var storage = await this.GetResourceAsync();

        // Create managed private endpoints
        string managedIntegrationRuntimeName;
        if (storage.Data.IsHnsEnabled.Equals(true))
        {
            managedIntegrationRuntimeName = await this.purviewAutomationClient.CreateManagedPrivateEndpointAsync(name: this.resource.Name, groupId: "dfs", resourceId: this.resourceId);
        }
        else
        {
            managedIntegrationRuntimeName = await this.purviewAutomationClient.CreateManagedPrivateEndpointAsync(name: this.resource.Name, groupId: "blob", resourceId: this.resourceId);
        }
        return managedIntegrationRuntimeName;
    }

    public async Task AddScanAsync(bool triggerScan, string managedIntegrationRuntimeName)
    {
        // Get resource
        var storage = await this.GetResourceAsync();

        // Create scan
        var scanName = "default";
        var scan = new
        {
            name = scanName,
            kind = storage.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2Msi" : "AzureStorageMsi",
            properties = new
            {
                scanRulesetName = storage.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2" : "AzureStorage",
                scanRulesetType = "System",
                collection = new
                {
                    referenceName = this.resource.ResourceGroupName,
                    type = "CollectionReference"
                },
                connectedVia = string.IsNullOrWhiteSpace(managedIntegrationRuntimeName) ? null : new
                {
                    referenceName = managedIntegrationRuntimeName
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
                includeUriPrefixes = new string[] { storage.Data.IsHnsEnabled.Equals(true) ? $"https://{this.resource.Name}.dfs.core.windows.net/" : $"https://{this.resource.Name}.blob.core.windows.net" }
            }
        };

        // Create scan
        if (triggerScan && string.IsNullOrWhiteSpace(managedIntegrationRuntimeName))
        {
            await this.purviewAutomationClient.AddScanAsync(dataSourceName: this.resource.Name, scan: scan, scanName: scanName, runScan: true, trigger: trigger, filter: filter);
        }
        else
        {
            await this.purviewAutomationClient.AddScanAsync(dataSourceName: this.resource.Name, scan: scan, scanName: scanName, runScan: false, trigger: trigger, filter: filter);
        }
    }

    public async Task RemoveDataSourceAsync()
    {
        // Remove data source
        await this.purviewAutomationClient.RemoveDataSourceAsync(dataSourceName: this.resource.Name);
    }

    public async Task OnboardDataSourceAsync(bool useManagedPrivateEndpoints, bool setupScan = true, bool triggerScan = true)
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
