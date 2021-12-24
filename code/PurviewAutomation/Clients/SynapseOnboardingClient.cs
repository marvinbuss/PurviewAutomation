using Azure.Analytics.Synapse.AccessControl;
using Azure.Analytics.Synapse.ManagedPrivateEndpoints;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using PurviewAutomation.Models.General;
using PurviewAutomation.Models.Purview;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class SynapseOnboardingClient : IDataSourceOnboardingClient, ILineageOnboardingClient
{
    private readonly string resourceId;
    public readonly ResourceIdentifier resource;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal SynapseOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
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
        var synapse = await this.GetResourceAsync();

        // Create data source
        var dataSource = new
        {
            name = this.resource.Name,
            kind = "AzureSynapseWorkspace",
            properties = new
            {
                resourceId = this.resourceId,
                subscriptionId = this.resource.SubscriptionId,
                resourceGroup = this.resource.ResourceGroupName,
                resourceName = this.resource.Name,
                serverlessSqlEndpoint = $"{this.resource.Name}-ondemand.sql.azuresynapse.net",
                dedicatedSqlEndpoint = $"{this.resource.Name}.sql.azuresynapse.net",
                location = synapse.Value.Data.Location.ToString(),
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
        var synapse = await this.GetResourceAsync();

        // Create scan
        var scanName = "default";
        var scan = new
        {
            name = scanName,
            kind = "AzureSynapseWorkspaceMsi",
            properties = new
            {
                // credential = null,
                collection = new
                {
                    referenceName = this.resource.ResourceGroupName,
                    type = "CollectionReference"
                },
                resourceTypes = new
                {
                    AzureSynapseServerlessSql = new
                    {
                        // credential = null,
                        resourceNameFilter = new
                        {
                            resources = new string[] { "default" }  // TODO: List DB Schema Sources (See: "{purviewId}/scan/datasources/{synapseName}/scans/_random/enumerateResources?api-version=2018-12-01-preview")
                        },
                        scanRulesetName = "AzureSynapseSQL",
                        scanRulesetType = "System"
                    }
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
        // Filters are already included in the scan object

        // Create scan
        if (triggerScan)
        {
            await this.purviewAutomationClient.AddScanAsync(dataSourceName: this.resource.Name, scan: scan, scanName: scanName, runScan: true, trigger: trigger, filter: null);
        }
    }

    public async Task RemoveDataSourceAsync()
    {
        // Remove data source
        await this.purviewAutomationClient.RemoveDataSourceAsync(dataSourceName: this.resource.Name);
    }

    public async Task AddManagedPrivateEndpointsAsync()
    {
        // Create client
        var managedPrivateEndpointsClient = new ManagedPrivateEndpointsClient(endpoint: new Uri(uriString: $"https://{this.resource.Name}.dev.azuresynapse.net"), credential: new DefaultAzureCredential());

        // Create managed private endpoints for Purview
        var privateEndpointDetails = new List<ManagedPrivateEndpointDetails>
        {
            new ManagedPrivateEndpointDetails{ Name = "Purview", GroupId = "account", ResourceId = this.purviewAutomationClient.resourceId },
            new ManagedPrivateEndpointDetails{ Name = "Purview_blob", GroupId = "blob", ResourceId = this.purviewAutomationClient.managedStorageResourceId },
            new ManagedPrivateEndpointDetails{ Name = "Purview_queue", GroupId = "queue", ResourceId = this.purviewAutomationClient.managedStorageResourceId },
            new ManagedPrivateEndpointDetails{ Name = "Purview_namespace", GroupId = "namespace", ResourceId = this.purviewAutomationClient.managedEventHubId }
        };
        foreach (var privateEndpointDetail in privateEndpointDetails)
        {
            try
            {
                await managedPrivateEndpointsClient.CreateAsync(
                    managedPrivateEndpointName: privateEndpointDetail.Name,
                    managedPrivateEndpoint: new Azure.Analytics.Synapse.ManagedPrivateEndpoints.Models.ManagedPrivateEndpoint
                    {
                        Properties = new Azure.Analytics.Synapse.ManagedPrivateEndpoints.Models.ManagedPrivateEndpointProperties
                        {
                            PrivateLinkResourceId = privateEndpointDetail.ResourceId,
                            GroupId = privateEndpointDetail.GroupId
                        }
                    },
                    managedVirtualNetworkName: "default"
                );
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: $"Private endpoint creation failed: {privateEndpointDetail}");
            }
        }
    }

    public async Task AddRoleAssignmentAsync(string principalId, SynapseRole role)
    {
        // Create client
        var roleAssignmentsClient = new RoleAssignmentsClient(endpoint: new Uri(uriString: $"https://{this.resource.Name}.dev.azuresynapse.net"), credential: new DefaultAzureCredential());

        // Get role
        var roleGuid = SynapseRoleConverter.ConvertRoleToGuid(role: role);

        // Create role assignment
        var roleAssignmentResponse = await roleAssignmentsClient.CreateRoleAssignmentAsync(roleAssignmentId: principalId, roleId: roleGuid, principalId: new Guid(principalId), scope: $"workspaces/{this.resource.Name}");
        this.logger.LogInformation($"Purview role assigment response: '{roleAssignmentResponse}'");
    }

    /// <summary>
    /// Adds Purview MSI identity as login to SQL Serverless. - blocked because of https://github.com/MicrosoftDocs/sql-docs/issues/2323
    /// </summary>
    /// <returns></returns>
    public async Task AddSqlPurviewLoginAsync()
    {
        try
        {
            using var connection = new SqlConnection(connectionString: $"Server=tcp:{this.resource.Name}-ondemand.sql.azuresynapse.net,1433;Initial Catalog=master;Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=\"Active Directory Default\";");
            await connection.OpenAsync();

            using var sqlCommand = new SqlCommand(cmdText: $"CREATE LOGIN [{this.purviewAutomationClient.resource.Name}] FROM EXTERNAL PROVIDER;", connection: connection);
            await sqlCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            this.logger.LogError(exception: ex, message: "Failed to add Purview MSI to SQL serverless databases");
        }
    }

    public async Task OnboardDataSourceAsync(bool setupScan = true, bool triggerScan = true)
    {
        await this.AddDataSourceAsync();

        if (setupScan)
        {
            // await this.AddSqlPurviewLoginAsync();
            // await this.AddScanAsync(triggerScan: triggerScan);
        }
    }

    public async Task OnboardLineageAsync(string principalId)
    {
        // Get resource
        var synapse = await this.GetResourceAsync();

        await this.AddRoleAssignmentAsync(principalId: principalId, role: SynapseRole.LinkedDataManager);
        await this.AddManagedPrivateEndpointsAsync();
        await this.purviewAutomationClient.AddRoleAssignmentAsync(principalId: synapse.Value.Data.Identity.SystemAssignedIdentity.PrincipalId.ToString(), role: PurviewRole.DataCurator);
    }
}
