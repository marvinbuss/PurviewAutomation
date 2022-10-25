using Azure;
using Azure.Analytics.Purview.Administration;
using Azure.Analytics.Purview.Scanning;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using PurviewAutomation.Models.General;
using PurviewAutomation.Models.Purview;
using PurviewAutomation.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal class PurviewAutomationClient
{
    public readonly string resourceId;
    public readonly ResourceIdentifier resource;
    public readonly string managedStorageResourceId;
    public readonly string managedEventHubId;
    private readonly string endpoint;
    private readonly string accountEndpoint;
    private readonly string scanEndpoint;
    private readonly string rootCollectionName;
    private readonly string rootCollectionPolicyId;
    public readonly string managedIntegrationRuntimeName;
    private readonly ILogger logger;

    internal PurviewAutomationClient(string resourceId, string managedStorageResourceId, string managedEventHubId, string rootCollectionName, string rootCollectionPolicyId, string managedIntegrationRuntimeName, ILogger logger)
    {
        if (resourceId.Split(separator: "/").Length != 9 ||
            managedStorageResourceId.Split(separator: "/").Length != 9 ||
            managedEventHubId.Split(separator: "/").Length != 9)
        {
            throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: nameof(resourceId));
        }
        this.resourceId = resourceId;
        this.resource = new ResourceIdentifier(resourceId: resourceId);
        this.managedStorageResourceId = managedStorageResourceId;
        this.managedEventHubId = managedEventHubId;
        this.endpoint = $"https://{this.resource.Name}.purview.azure.com";
        this.accountEndpoint = $"https://{this.resource.Name}.purview.azure.com/account";
        this.scanEndpoint = $"https://{this.resource.Name}.purview.azure.com/scan";
        this.rootCollectionName = rootCollectionName;
        this.rootCollectionPolicyId = rootCollectionPolicyId;
        this.logger = logger;
        this.managedIntegrationRuntimeName = managedIntegrationRuntimeName;
    }

    internal async Task<GenericResource> GetResourceAsync()
    {
        // Create client
        var armClient = new ArmClient(credential: new DefaultAzureCredential());

        // Get resource
        var resource = await armClient.GetGenericResource(id: new ResourceIdentifier(resourceId: this.resourceId)).GetAsync();

        return resource.Value;
    }

    private async Task AddCollectionsAsync(string subscriptionId, string resourceGroupName)
    {
        // Create client
        var accountClient = new PurviewAccountClient(endpoint: new Uri(this.accountEndpoint), credential: new DefaultAzureCredential());

        // Create collections
        var collectionDetails = new List<CollectionDetails>
        {
            new CollectionDetails{ Name = subscriptionId, Description = $"Collection for data sources in Subscription '{subscriptionId}'", ParentCollectionName = this.rootCollectionName },
            new CollectionDetails{ Name = resourceGroupName, Description = $"Collection for data sources in Subscription '{subscriptionId}' and Resource Group '{resourceGroupName}'", ParentCollectionName = subscriptionId }
        };
        foreach (var item in collectionDetails)
        {
            var collectionClient = accountClient.GetCollectionClient(collectionName: item.Name);
            var collection = new
            {
                name = item.Name,
                description = item.Description,
                parentCollection = new
                {
                    referenceName = item.ParentCollectionName,
                    type = "CollectionReference"
                }
            };

            try
            {
                var response = await collectionClient.CreateOrUpdateCollectionAsync(content: RequestContent.Create(serializable: collection));
                this.logger.LogInformation(message: $"Purview Collection creation response {response}");

                if (response.IsError)
                {
                    throw new RequestFailedException("Failed to create the Purview Collection");
                }
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogError(exception: ex, message: $"Creation of Purview Collection '{item.Name}' failed: '{ex.Message}'");
                throw;
            }
        }
    }

    internal async Task AddDataSourceAsync(string subscriptionId, string resourceGroupName, string dataSourceName, object dataSource)
    {
        // Create collections
        await this.AddCollectionsAsync(subscriptionId: subscriptionId, resourceGroupName: resourceGroupName);

        // Create client
        var dataSourceClient = new PurviewDataSourceClient(endpoint: new Uri(uriString: this.scanEndpoint), dataSourceName: dataSourceName, credential: new DefaultAzureCredential());

        // Add data source
        try
        {
            var response = await dataSourceClient.CreateOrUpdateAsync(content: RequestContent.Create(serializable: dataSource));
            this.logger.LogInformation(message: $"Purview Data Source creation response: '{response}'");

            if (response.IsError)
            {
                throw new RequestFailedException("Failed to create the Purview Data Source");
            }
        }
        catch (RequestFailedException ex)
        {
            this.logger.LogError(exception: ex, message: $"Purview Data Source creation of resource '{dataSourceName}' failed: '{ex.Message}'");
            throw;
        }
    }

    internal async Task AddScanAsync(string dataSourceName, object scan, string scanName = "default", bool runScan = false, object trigger = null, object filter = null)
    {
        // Create client
        var scanClient = new PurviewScanClient(endpoint: new Uri(uriString: this.scanEndpoint), dataSourceName: dataSourceName, scanName: scanName, credential: new DefaultAzureCredential());

        // Create scan
        var scanResponse = await scanClient.CreateOrUpdateAsync(content: RequestContent.Create(serializable: scan));
        this.logger.LogInformation(message: $"Purview scan creation response: '{scanResponse}'");

        // Create trigger
        if (trigger != null)
        {
            try
            {
                var triggerResponse = await scanClient.CreateOrUpdateTriggerAsync(content: RequestContent.Create(serializable: trigger));
                this.logger.LogInformation(message: $"Purview trigger creation response: '{triggerResponse}'");

                if (triggerResponse.IsError)
                {
                    throw new RequestFailedException("Failed to create the trigger");
                }
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogError(exception: ex, message: $"Failed to create trigger for '{dataSourceName}' with error: '{ex.Message}'");
                throw;
            }
        }

        // Create filter
        if (filter != null)
        {
            try
            {
                var filterResponse = await scanClient.CreateOrUpdateFilterAsync(content: RequestContent.Create(serializable: filter));
                this.logger.LogInformation(message: $"Purview filter creation response: '{filterResponse}'");

                if (filterResponse.IsError)
                {
                    throw new RequestFailedException("Failed to create the filter");
                }
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogError(exception: ex, message: $"Failed to create filter for '{dataSourceName}' with error: '{ex.Message}'");
                throw;
            }
        }

        // Run scan
        if (runScan)
        {
            try
            {
                var scanRunResponse = await scanClient.RunScanAsync(runId: Guid.NewGuid().ToString(), options: new Azure.RequestOptions(), scanLevel: "Full");
                this.logger.LogInformation(message: $"Purview scan run creation response: '{scanRunResponse}'");

                if (scanRunResponse.IsError)
                {
                    throw new RequestFailedException("Failed to create the scan");
                }
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogError(exception: ex, message: $"Failed to create scan for '{dataSourceName}' with error: '{ex.Message}'");
                throw;
            }
        }
    }

    internal async Task<JsonElement> GetDataSourceAsync(string dataSourceName)
    {
        // Create client
        var dataSourceClient = new PurviewDataSourceClient(endpoint: new Uri(uriString: this.scanEndpoint), dataSourceName: dataSourceName, credential: new DefaultAzureCredential());

        try
        {
            var response = await dataSourceClient.GetPropertiesAsync(new ());
            this.logger.LogInformation(message: $"Purview Data Source get response: '{response}'");

            if (response.IsError)
            {
                throw new RequestFailedException("Failed to obtain details of the Purview Data Source");
            }

            using var jsonDocument = JsonDocument.Parse(Utils.GetContentFromResponse(response));
            var jsonBody = jsonDocument.RootElement;
            return jsonBody;
        }
        catch (RequestFailedException ex)
        {
            this.logger.LogError(exception: ex, message: $"Purview Data Source details of resource '{dataSourceName}' could not be loaded: '{ex.Message}'");
        }
        return new JsonElement();
    }

    internal async Task RemoveDataSourceAsync(string dataSourceName)
    {
        // Create client
        var dataSourceClient = new PurviewDataSourceClient(endpoint: new Uri(uriString: this.scanEndpoint), dataSourceName: dataSourceName, credential: new DefaultAzureCredential());

        // Remove data source
        try
        {
            var response = await dataSourceClient.DeleteAsync();
            this.logger.LogInformation(message: $"Purview Data Source deletion response: '{response}'");
            
            if (response.IsError)
            {
                throw new RequestFailedException(status: response.Status, message: "Failed to delete the Purview Data Source");
            }
        }
        catch (RequestFailedException ex)
        {
            this.logger.LogError(exception: ex, message: $"Purview Data Source deletion of resource '{dataSourceName}' unsuccessful because it was already removed: '{ex.Message}'");
            if (ex.Status != 404)
            {
                throw;
            }
        }
    }

    internal async Task AddRoleAssignmentAsync(string principalId, PurviewRole role)
    {
        // Create client
        var metadataPolicyClient = new PurviewMetadataPolicyClient(endpoint: new Uri(uriString: this.endpoint), collectionName: this.rootCollectionName, credential: new DefaultAzureCredential());

        // Get role
        var roleString = PurviewRoleConverter.ConvertRoleToString(role: role);

        try
        {
            // Get metadata policy
            var metadataPolicyResponse = await metadataPolicyClient.GetMetadataPolicyAsync(policyId: this.rootCollectionPolicyId, options: new());

            if (metadataPolicyResponse.IsError)
            {
                throw new RequestFailedException("Failed to get the Purview metadata policy");
            }

            var metadataPolicyJson = JsonDocument.Parse(utf8Json: Utils.GetContentFromResponse(r: metadataPolicyResponse)).RootElement;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var metadataPolicyObject = JsonSerializer.Deserialize<MetadataPolicy>(element: metadataPolicyJson, options: options);

            // Add principal Id
            foreach (var attributerule in metadataPolicyObject.Properties.AttributeRules)
            {
                if (attributerule.Id.StartsWith(roleString))
                {
                    foreach (var dnfCondition in attributerule.DnfCondition[0])
                    {
                        if (dnfCondition.AttributeName.Equals("principal.microsoft.id"))
                        {
                            dnfCondition.AttributeValueIncludedIn?.Add(principalId);
                        }
                    }
                }
            }

            // Create role asignment
            var metadataPolicyUpdateResponse = await metadataPolicyClient.UpdateMetadataPolicyAsync(policyId: this.rootCollectionPolicyId, content: RequestContent.Create(serializable: metadataPolicyObject));
            this.logger.LogInformation(message: $"Purview collection role assignment response: '{metadataPolicyUpdateResponse}'");

            if (metadataPolicyUpdateResponse.IsError)
            {
                throw new RequestFailedException("Failed to create the role assignment");
            }
        }
        catch (RequestFailedException ex)
        {
            this.logger.LogError(exception: ex, message: $"Purview role assignment to collection '{rootCollectionName}' unsuccessful: '{ex.Message}'");
            throw;
        }
    }

    internal async Task<string> CreateManagedPrivateEndpointAsync(string name, string groupId, string resourceId)
    {
        // Get access token
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(requestContext: new TokenRequestContext(scopes: new string[] { "https://purview.azure.net/.default" }));

        // Create client
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.Token);
        client.BaseAddress = new Uri(uriString: $"https://{this.resource.Name}.purview.azure.com/proxy/");

        // Create managed vnet
        var managedVnet = new
        {
            name = "default",
            properties = new { }
        };
        var requestUriManagedVnet = client.BaseAddress.ToString() + $"managedVirtualNetworks/{managedVnet.name}?api-version=2020-12-01-preview";
        var requestManagedVnet = new HttpRequestMessage(method: HttpMethod.Put, requestUri: requestUriManagedVnet)
        {
            Content = new StringContent(content: JsonSerializer.Serialize(managedVnet), encoding: Encoding.UTF8, mediaType: "application/json")
        };
        var successManagedVnet = await this.MakeRequestAsync(client: client, request: requestManagedVnet);
        if (!successManagedVnet)
        {
            logger.LogError(message: "Creation of managed virtual network failed.");
            return null;
        }

        // Get or create managed integration runtime
        var managedIr = new
        {
            name = this.managedIntegrationRuntimeName,
            properties = new
            {
                description = "Default Integration Runtime",
                type = "Managed",
                typeProperties = new
                {
                    computeProperties = new
                    {
                        location = "AutoResolve"
                    }
                },
                managedVirtualNetwork = new
                {
                    referenceName = "default",
                    type = "ManagedVirtualNetworkReference"
                }
            }
        };
        var requestUriManagedIr = client.BaseAddress.ToString() + $"integrationRuntimes/{managedIr.name}?api-version=2020-12-01-preview";
        var requestManagedIr = new HttpRequestMessage(method: HttpMethod.Put, requestUri: requestUriManagedIr)
        {
            Content = new StringContent(content: JsonSerializer.Serialize(managedIr), encoding: Encoding.UTF8, mediaType: "application/json")
        };
        var successManagedIr = await this.MakeRequestAsync(client: client, request: requestManagedIr);
        if (!successManagedIr)
        {
            logger.LogError(message: "Creation of managed integration runtime failed.");
            return null;
        }

        // Create managed private endpoints
        var successesManagedPrivateEndpoint = new List<bool>();
        var privateEndpointDetails = new List<ManagedPrivateEndpointDetails>
        {
            new ManagedPrivateEndpointDetails{ Name = $"{this.resource.Name}-account", GroupId = "account", ResourceId = this.resourceId },
            new ManagedPrivateEndpointDetails{ Name = $"{this.resource.Name}-managedBlob", GroupId = "blob", ResourceId = this.managedStorageResourceId },
            new ManagedPrivateEndpointDetails{ Name = $"{this.resource.Name}-managedQueue", GroupId = "queue", ResourceId = this.managedStorageResourceId },
            new ManagedPrivateEndpointDetails{ Name = name, GroupId = groupId, ResourceId = resourceId }
        };
        foreach (var privateEndpointDetail in privateEndpointDetails)
        {
            var managedPrivateEndpoint = new
            {
                name = privateEndpointDetail.Name,
                properties = new
                {
                    groupId = privateEndpointDetail.GroupId,
                    privateLinkResourceId = privateEndpointDetail.ResourceId
                }
            };
            var requestUriManagedPrivateEndpoint = client.BaseAddress.ToString() + $"managedVirtualNetworks/default/managedPrivateEndpoints/{privateEndpointDetail.Name}?api-version=2020-12-01-preview";
            var requestManagedPrivateEndpoint = new HttpRequestMessage(method: HttpMethod.Put, requestUri: requestUriManagedPrivateEndpoint)
            {
                Content = new StringContent(content: JsonSerializer.Serialize(managedPrivateEndpoint), encoding: Encoding.UTF8, mediaType: "application/json")
            };
            var successManagedPrivateEndpoint = await this.MakeRequestAsync(client: client, request: requestManagedPrivateEndpoint);
            if (!successManagedPrivateEndpoint)
            {
                logger.LogError(message: $"Creation of managed private endpoint '{privateEndpointDetail.Name}' for resource '{privateEndpointDetail.ResourceId}' and groupId '{privateEndpointDetail.GroupId}' failed.");
                successesManagedPrivateEndpoint.Add(item: false);
            }
            else
            {
                successesManagedPrivateEndpoint.Add(item: true);
            }
        }
        if (successesManagedPrivateEndpoint.TrueForAll(match: b => b))
        {
            return managedIr.name;
        }
        return null;
    }

    private async Task<bool> MakeRequestAsync(HttpClient client, HttpRequestMessage request)
    {
        var response = await client.SendAsync(request: request);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogError(exception: ex, message: "HTTP Request failed");
            return false;
        }
        return true;
    }
}
