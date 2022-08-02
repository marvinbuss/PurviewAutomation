using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using PurviewAutomation.Models.General;
using System;
using System.Collections.Generic;
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

    private async Task<GenericResource> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get resource
        var resource = await armClient.GetGenericResource(id: new ResourceIdentifier(resourceId: this.resourceId)).GetAsync();

        return resource.Value;
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
                endpoint = $"https://{this.resource.Name}.{kusto.Data.Location}.kusto.windows.net",
                location = kusto.Data.Location.ToString(),
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
        return null;
    }

    public async Task AddScanAsync(bool triggerScan, string managedIntegrationRuntimeName)
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
                endpoint = $"https://{this.resource.Name}.{kusto.Data.Location}.kusto.windows.net",
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
                includeUriPrefixes = new string[] { $"https://{this.resource.Name}.{kusto.Data.Location}.kusto.windows.net/" }
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

    internal async Task AddRoleAssignmentAsync(string principalId, KustoRole role)
    {
        // Get resource
        var kusto = await this.GetResourceAsync();
        var roleAssignmentResourceId = new ResourceIdentifier(resourceId: $"{this.resourceId}/principalAssignments/{Guid.NewGuid()}");

        // Create client
        var armClientOptions = new ArmClientOptions();
        this.logger.LogInformation($"Resource Type: {roleAssignmentResourceId.ResourceType}");
        armClientOptions.SetApiVersion(resourceType: roleAssignmentResourceId.ResourceType, apiVersion: "2022-02-01");
        var armClient = new ArmClient(credential: new DefaultAzureCredential(), defaultSubscriptionId: this.resource.SubscriptionId, options: armClientOptions);

        // Get role
        var roleString = KustoRoleConverter.ConvertRoleToString(role: role);

        // Get tenant Id
        string tenantId = "";
        var tenantList = armClient.GetTenants().GetAll();
        foreach (var tenant in tenantList)
        {
            tenantId = tenant.Data.TenantId.ToString();
            break;
        }

        // Create cluster role assignment
        var genericResources = armClient.GetGenericResources();
        var principalAssignmentResourceData = new GenericResourceData(location: kusto.Data.Location)
        {
            Properties = BinaryData.FromObjectAsJson(new Dictionary<string, object>()
            {
                { "principalId", principalId },
                { "principalType", "App" },
                { "role", roleString },
                { "tenantId", tenantId }
            })
        };

        await genericResources.CreateOrUpdateAsync(waitUntil: Azure.WaitUntil.Completed, resourceId: roleAssignmentResourceId, data: principalAssignmentResourceData);
    }

    public async Task OnboardDataSourceAsync(bool useManagedPrivateEndpoints, bool setupScan, bool triggerScan)
    {
        await this.AddDataSourceAsync();

        string managedIntegrationRuntimeName = string.Empty;
        if (useManagedPrivateEndpoints)
        {
            // managedIntegrationRuntimeName = await this.AddScanningManagedPrivateEndpointsAsync();
        }
        if (setupScan)
        {
            var purview = await this.purviewAutomationClient.GetResourceAsync();
            await this.AddRoleAssignmentAsync(principalId: purview.Data.Identity.PrincipalId.ToString(), role: KustoRole.AllDatabasesViewer);
            await this.AddScanAsync(triggerScan: triggerScan, managedIntegrationRuntimeName: managedIntegrationRuntimeName);
        }
    }
}
