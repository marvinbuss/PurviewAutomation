using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients
{
    internal class StorageOnboardingClient : IDataSourceOnboardingClient
    {
        private readonly string name;
        private readonly string resourceId;
        private readonly string subscriptionId;
        private readonly string resourceGroupName;
        private readonly PurviewAutomationClient purviewAutomationClient;

        internal StorageOnboardingClient(string resourceId, PurviewAutomationClient client)
        {
            if (resourceId.Split(separator: "/").Length != 9)
            {
                throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: nameof(resourceId));
            }
            this.name = resourceId.Split(separator: "/")[8];
            this.subscriptionId = resourceId.Split(separator: "/")[2];
            this.resourceGroupName = resourceId.Split(separator: "/")[4];
            this.purviewAutomationClient = client;
        }

        private async Task<Azure.Response<StorageAccount>> GetStorageAsync()
        {
            // Create client
            var armClient = new ArmClient(credential: new DefaultAzureCredential()); 
            
            // Get storage
            var resourceGroup = armClient.GetResourceGroup(id: new ResourceIdentifier(resourceId: $"/subscriptions/{this.subscriptionId}/resourceGroups/{this.resourceGroupName}"));
            return await resourceGroup.GetStorageAccounts().GetAsync(accountName: name);
        }

        public async Task AddDataSourceAsync()
        {
            // Get storage
            var storage = await this.GetStorageAsync();

            // Create data source
            var dataSource = new
            {
                name = this.name,
                kind = storage.Value.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2" : "AzureStorage",
                properties = new
                {
                    resourceId = resourceId,
                    subscriptionId = this.subscriptionId,
                    resourceGroup = this.resourceGroupName,
                    resourceName = this.name,
                    endpoint = storage.Value.Data.IsHnsEnabled.Equals(true) ? $"https://{this.name}.dfs.core.windows.net/" : $"https://{this.name}.blob.core.windows.net/",
                    location = storage.Value.Data.Location.ToString(),
                    collection = new
                    {
                        referenceName = this.resourceGroupName,
                        type = "CollectionReference"
                    }
                }
            };

            // Add data source
            await this.purviewAutomationClient.AddDataSourceAsync(dataSourceName: this.name, dataSource: dataSource);
        }

        public async Task AddScanAsync()
        {
            // Get storage
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
                        referenceName = resourceGroupName,
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
                    includeUriPrefixes = new string[] { storage.Value.Data.IsHnsEnabled.Equals(true) ? $"https://{this.name}.dfs.core.windows.net/" : $"https://{this.name}.blob.core.windows.net" }
                }
            };

            // Create scan
            await this.purviewAutomationClient.AddScanAsync(dataSourceName: this.name, scan: scan, scanName: scanName, runScan: true, trigger: trigger, filter: filter);
        }

        public async Task RemoveDataSourceAsync()
        {
            // Remove data source
            await this.purviewAutomationClient.RemoveDataSourceAsync(dataSourceName: this.name);
        }

        public async Task OnboardDataSource()
        {
            await this.purviewAutomationClient.CreateCollectionsAsync(subscriptionId: this.subscriptionId, resourceGroupName: this.resourceGroupName);
            await this.AddDataSourceAsync();
            await this.AddScanAsync();
        }
    }
}
