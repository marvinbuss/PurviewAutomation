using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using PurviewAutomation.Models.General;
using System;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class KustoOnboardingClient : IDataSourceOnboardingClient
{
    private readonly string resourceId;
    private readonly ResourceIdentifier resource;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal KustoOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
    {
        if (resourceId.Split(separator: "/").Length != 9)
        {
            throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: nameof(resourceId));
        }
        this.resourceId = resourceId.Replace(oldValue: "/Clusters/", newValue: "/clusters/");
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
        var kusto = await this.GetResourceAsync();

        // Create data source
        var dataSource = new
        {
            name = this.resource.Name,
            kind = "AzureDataExplorer",
            properties = new
            {
                resourceId = this.resourceId,
                subscriptionId = this.resource.SubscriptionId,
                resourceGroup = this.resource.ResourceGroupName,
                resourceName = this.resource.Name,
                endpoint = $"https://{this.resource.Name}.{kusto.Value.Data.Location}.kusto.windows.net",
                location = kusto.Value.Data.Location.ToString(),
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
        // Get resource
        var kusto = await this.GetResourceAsync();

        // Create scan
        var scanName = "default";
        var scan = new
        {
            name = scanName,
            kind = "AzureDataExplorerMsi",
            properties = new
            {
                scanRulesetName = "AzureDataExplorer",
                scanRulesetType = "System",
                endpoint = $"https://{this.resource.Name}.{kusto.Value.Data.Location}.kusto.windows.net",
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
                includeUriPrefixes = new string[] { $"https://{this.resource.Name}.{kusto.Value.Data.Location}.kusto.windows.net/" }
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

    internal async Task AddRoleAssignmentAsync(string principalId, KustoRole role)
    {
        throw new NotImplementedException();

        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get role
        var roleString = KustoRoleConverter.ConvertRoleToString(role: role);

        // TODO: Create role assignment via ARM
    }

    public async Task OnboardDataSourceAsync(bool setupScan, bool triggerScan)
    {
        await this.AddDataSourceAsync();

        if (setupScan)
        {
            // var purview = await this.purviewAutomationClient.GetResourceAsync();
            // await this.AddRoleAssignmentAsync(principalId: purview.Value.Data.Identity.SystemAssignedIdentity.PrincipalId.ToString(), role: KustoRole.AllDatabasesViewer);
            // await this.AddScanAsync(triggerScan: triggerScan);
        }
    }
}
