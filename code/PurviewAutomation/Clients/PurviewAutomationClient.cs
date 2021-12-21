using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Azure.Core;
using Azure.Identity;
using Azure.Analytics.Purview.Administration;
using Azure.Analytics.Purview.Scanning;
using PurviewAutomation.Models.Purview;
using PurviewAutomation.Utility;

namespace PurviewAutomation.Clients;

internal class PurviewAutomationClient
{
    private readonly string name;
    private readonly string resourceId;
    private readonly string managedStorageResourceId;
    private readonly string managedEventHubId;
    private readonly string endpoint;
    private readonly string accountEndpoint;
    private readonly string scanEndpoint;
    private readonly string rootCollectionName;
    private readonly string rootCollectionPolicyId;
    private readonly ILogger logger;

    internal PurviewAutomationClient(string resourceId, string managedStorageResourceId, string managedEventHubId, string rootCollectionName, string rootCollectionPolicyId, ILogger logger)
    {
        if (resourceId.Split(separator: "/").Length != 9 ||
            managedStorageResourceId.Split(separator: "/").Length != 9 ||
            managedEventHubId.Split(separator: "/").Length != 9)
        {
            throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: nameof(resourceId));
        }
        this.name = resourceId.Split(separator: "/")[8];
        this.resourceId = resourceId;
        this.managedStorageResourceId = managedStorageResourceId;
        this.managedEventHubId = managedEventHubId;
        this.endpoint = $"https://{this.name}.purview.azure.com";
        this.accountEndpoint = $"https://{this.name}.purview.azure.com/account";
        this.scanEndpoint = $"https://{this.name}.purview.azure.com/scan";
        this.rootCollectionName = rootCollectionName;
        this.rootCollectionPolicyId = rootCollectionPolicyId;
        this.logger = logger;
    }

    internal async Task CreateCollectionsAsync(string subscriptionId, string resourceGroupName)
    {
        // Create client
        var accountClient = new PurviewAccountClient(endpoint: new Uri(this.accountEndpoint), credential: new DefaultAzureCredential());

        // Create collections
        var collectionDetails = new List<CollectionDetails>
        {
            new CollectionDetails{ Name = subscriptionId, Description = $"Collection for data sources in Subscription '{subscriptionId}'", ParentCollectionName = this.rootCollectionName },
            new CollectionDetails{ Name = resourceGroupName, Description = $"Collection for data sources in Subscription '{subscriptionId}' and Resource Group '{resourceGroupName}'", ParentCollectionName = subscriptionId }
        };
        foreach(var item in collectionDetails)
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
            var response = await collectionClient.CreateOrUpdateCollectionAsync(content: RequestContent.Create(serializable: collection));
            this.logger.LogInformation($"Purview Collection creation response {response}");
        }
    }

    internal async Task AddDataSourceAsync(string dataSourceName, object dataSource)
    {
        // Create client
        var dataSourceClient = new PurviewDataSourceClient(endpoint: new Uri(uriString: this.scanEndpoint), dataSourceName: dataSourceName, credential: new DefaultAzureCredential());

        // Add data source
        var response = await dataSourceClient.CreateOrUpdateAsync(content: RequestContent.Create(serializable: dataSource));
        this.logger.LogInformation($"Purview Data Source creation response: '{response}'");
    }

    internal async Task RemoveDataSourceAsync(string dataSourceName)
    {
        // Create client
        var dataSourceClient = new PurviewDataSourceClient(endpoint: new Uri(uriString: this.scanEndpoint), dataSourceName: dataSourceName, credential: new DefaultAzureCredential());

        // Remove data source
        var response = await dataSourceClient.DeleteAsync();
        this.logger.LogInformation($"Purview Data Source deletion response: '{response}'");
    }

    internal async Task AddRoleAssignmentAsync(string principalId, Role role)
    {
        // Create client
        var metadataPolicyClient = new PurviewMetadataPolicyClient(endpoint: new Uri(uriString: this.endpoint), collectionName: this.rootCollectionName, credential: new DefaultAzureCredential());

        // Get role
        var roleString = RoleConverter.ConvertRoleToString(role: role);

        // Get metadata policy
        var metadataPolicy = await metadataPolicyClient.GetMetadataPolicyAsync(policyId: this.rootCollectionPolicyId, options: new ());
        var metadataPolicyJson = JsonDocument.Parse(utf8Json: Utils.GetContentFromResponse(metadataPolicy)).RootElement;
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
        var response = metadataPolicyClient.UpdateMetadataPolicyAsync(policyId: this.rootCollectionPolicyId, content: RequestContent.Create(serializable: metadataPolicyObject));
        this.logger.LogInformation($"Purview collection role assignment response: '{response}'");
    }
}
