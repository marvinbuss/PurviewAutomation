using Azure.Analytics.Synapse.AccessControl;
using Azure.Analytics.Synapse.ManagedPrivateEndpoints;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.Logging;
using PurviewAutomation.Models.General;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class SynapseOnboardingClient : IDataSourceOnboardingClient
{
    private readonly string name;
    private readonly string resourceId;
    private readonly string subscriptionId;
    private readonly string resourceGroupName;
    private readonly PurviewAutomationClient purviewAutomationClient;
    private readonly ILogger logger;

    internal SynapseOnboardingClient(string resourceId, PurviewAutomationClient client, ILogger logger)
    {
        if (resourceId.Split(separator: "/").Length != 9)
        {
            throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: nameof(resourceId));
        }
        this.name = resourceId.Split(separator: "/")[8];
        this.subscriptionId = resourceId.Split(separator: "/")[2];
        this.resourceGroupName = resourceId.Split(separator: "/")[4];
        this.purviewAutomationClient = client;
        this.logger = logger;
    }

    private async Task<Azure.Response<Azure.ResourceManager.Resources.GenericResource>> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get resource
        return await armClient.GetGenericResource(id: new ResourceIdentifier(resourceId: resourceId)).GetAsync();
    }

    public async Task AddDataSourceAsync()
    {
        // Get resource
        var synapse = await this.GetResourceAsync();

        // Create data source
        var dataSource = new
        {
            name = this.name,
            kind = "AzureSynapseWorkspace",
            properties = new
            {
                resourceId = this.resourceId,
                subscriptionId = this.subscriptionId,
                resourceGroup = this.resourceGroupName,
                resourceName = this.name,
                serverlessSqlEndpoint = $"{this.name}-ondemand.sql.azuresynapse.net",
                dedicatedSqlEndpoint = $"{this.name}.sql.azuresynapse.net",
                location = synapse.Value.Data.Location.ToString(),
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

    public Task AddScanAsync()
    {
        throw new NotImplementedException();
    }

    public async Task RemoveDataSourceAsync()
    {
        // Remove data source
        await this.purviewAutomationClient.RemoveDataSourceAsync(dataSourceName: this.name);
    }

    public async Task AddManagedPrivateEndpoints()
    {
        // Create client
        var managedPrivateEndpointsClient = new ManagedPrivateEndpointsClient(endpoint: new Uri(uriString: $"https://{this.name}.dev.azuresynapse.net"), credential: new DefaultAzureCredential());

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
        var roleAssignmentsClient = new RoleAssignmentsClient(endpoint: new Uri(uriString: $"https://{this.name}.dev.azuresynapse.net"), credential: new DefaultAzureCredential());

        // Get role
        var roleGuid = SynapseRoleConverter.ConvertRoleToGuid(role: role);

        // Create role assignment
        var roleAssignmentResponse = await roleAssignmentsClient.CreateRoleAssignmentAsync(roleAssignmentId: principalId, roleId: roleGuid, principalId: new Guid(principalId), scope: $"workspaces/{this.name}");
        this.logger.LogInformation($"Purview role assigment response: '{roleAssignmentResponse}'");
    }

    public Task OnboardDataSource()
    {
        throw new NotImplementedException();
    }
}
