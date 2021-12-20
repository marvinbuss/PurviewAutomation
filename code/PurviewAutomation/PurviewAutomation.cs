// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Analytics.Purview.Scanning;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Analytics.Synapse.ManagedPrivateEndpoints;
using Azure.Analytics.Synapse.AccessControl;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Text.Json;
using Azure.Analytics.Purview.Administration;

using PurviewAutomation.Models;
using System.IO;
using Azure;

namespace PurviewAutomation
{
    public static class PurviewAutomation
    {
        [FunctionName("PurviewAutomation")]
        public static void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation("Parsing Event Grid event data");
            var eventGridEventString = eventGridEvent.Data.ToString();
            var eventGridEventJsonObject = JsonNode.Parse(eventGridEventString);

            // Check if Event Grid event has state succeeded
            var eventGridEventStatus = eventGridEventJsonObject["status"].ToString().ToLower();
            if (eventGridEventStatus != "succeeded")
            {
                log.LogError($"Received Event Grid event in a non succeeded state (State: {eventGridEventStatus})");
                throw new Exception("Received Event Grid event in a non succeeded state");
            }

            // Parsing Event Grid details
            var eventGridEventAction = eventGridEventJsonObject["authorization"]["action"].ToString();
            var eventGridEventScope = eventGridEventJsonObject["authorization"]["scope"].ToString();

            // Parsing scope details and get env variables
            var eventGridEventScopeArray = eventGridEventScope.Split(separator: "/");
            if (eventGridEventScopeArray.Length < 9)
            {
                log.LogError($"Incorrect scope length (Length: {eventGridEventScopeArray.Length}, Scope: {eventGridEventScope})");
                throw new Exception("Incorrect scope length");
            }
            var purviewResourceId = GetEnvironmentVariable(name: "PurviewResourceId");
            var purviewManagedStorageId = GetEnvironmentVariable(name: "PurviewManagedStorageId");
            var purviewManagedEventHubId = GetEnvironmentVariable(name: "PurviewManagedEventHubId");
            var purviewAccountName = purviewResourceId.Split(separator: "/")[8];
            var purviewAccountEndpoint = $"https://{purviewAccountName}.purview.azure.com/account";
            var purviewScanEndpoint = $"https://{purviewAccountName}.purview.azure.com/scan";
            var purviewRootCollectionName = GetEnvironmentVariable(name: "PurviewRootCollectionName");
            var subscriptionId = eventGridEventScopeArray[2];
            var resourceGroupName = eventGridEventScopeArray[4];
            var resourceName = eventGridEventScopeArray[8];

            // Check if Event Grid event action is supported and onboard dataset
            switch (eventGridEventAction)
            {
                case "Microsoft.Storage/storageAccounts/write":
                    log.LogInformation("Storage Account creation detected");
                    PurviewCollectionSetup(subscriptionId: subscriptionId, resourceGroupName: resourceGroupName, purviewRootCollectionName: purviewRootCollectionName, purviewAccountEndpoint: purviewAccountEndpoint, log: log);
                    CreateStorageAccount(resourceId: eventGridEventScope, subscriptionId: subscriptionId, resourceGroupName: resourceGroupName, resourceName: resourceName, purviewScanEndpoint: purviewScanEndpoint, log: log);
                    break;
                case "Microsoft.Storage/storageAccounts/delete":
                    log.LogInformation("Storage Account deletion detected");
                    DeleteDataSource(resourceName: resourceName, purviewScanEndpoint: purviewScanEndpoint, log: log);
                    break;
                case "Microsoft.Synapse/workspaces/write":
                    log.LogInformation("Synapse workspace creation detected");
                    PurviewCollectionSetup(subscriptionId: subscriptionId, resourceGroupName: resourceGroupName, purviewRootCollectionName: purviewRootCollectionName, purviewAccountEndpoint: purviewAccountEndpoint, log: log);
                    CreateSynapseWorkspace(resourceId: eventGridEventScope, subscriptionId: subscriptionId, resourceGroupName: resourceGroupName, resourceName: resourceName, purviewScanEndpoint: purviewScanEndpoint, purviewAccountName: purviewAccountName, purviewResourceId: purviewResourceId, purviewManagedStorageId: purviewManagedStorageId, purviewManagedEventHubId: purviewManagedEventHubId, log: log);
                    break;
                case "Microsoft.Synapse/workspaces/delete":
                    log.LogInformation("Synapse workspace deletion detected");
                    DeleteDataSource(resourceName: resourceName, purviewScanEndpoint: purviewScanEndpoint, log: log);
                    break;
                default:
                    log.LogInformation($"Unsupported Event Grid action detected: '{eventGridEventAction}'");
                    break;
            }
        }

        /// <summary> 
        /// Creates or updates collections below the provided Purview Root collection.
        /// </summary>
        /// <param name="subscriptionId">Subscription ID of the resource.</param>
        /// <param name="resourceGroupName">Resource Group Name of the Resource.</param>
        /// <param name="purviewRootCollectionName">Name of the root collection in Purview.</param>
        /// <param name="purviewAccountEndpoint">Account Endpoint of the Purview account.</param>
        /// <param name="log">Logger object to capture logs.</param>
        /// <remarks>
        /// The Purview Root collection name is provided as environment variable via application settings (Application Setting 'PurviewRootCollectionName').
        /// A first Purview sub-collection for the subscription is created below the Purview root collection.
        /// A second Purview sub-collection for the resource group is created below the subscription sub-collection.
        /// </remarks>
        /// <exception cref="Exception">Throws an exception if the scope is incorrect.</exception>
        private static void PurviewCollectionSetup(string subscriptionId, string resourceGroupName, string purviewRootCollectionName, string purviewAccountEndpoint, ILogger log)
        {
            // Create Purview account client
            var endpoint = new Uri(uriString: purviewAccountEndpoint);
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var purviewClient = new PurviewAccountClient(endpoint: endpoint, credential: credential);

            // Create or Update Purview Collection for subscription
            var collectionClient = purviewClient.GetCollectionClient(collectionName: subscriptionId);
            var collectionDetails = new
            {
                name = subscriptionId,
                description = $"Collection for data sources in Subscription '{subscriptionId}'",
                parentCollection = new
                {
                    referenceName = purviewRootCollectionName,
                    type = "CollectionReference"
                }
            };
            var content = RequestContent.Create(serializable: collectionDetails);
            var response = collectionClient.CreateOrUpdateCollection(content: content);
            log.LogInformation($"Purview Collection creation response {response}");

            // Create or Update Purview Collection for resource group
            collectionClient = purviewClient.GetCollectionClient(collectionName: resourceGroupName);
            collectionDetails = new
            {
                name = resourceGroupName,
                description = $"Collection for data sources in Subscription '{subscriptionId}' and Resource Group '{resourceGroupName}'",
                parentCollection = new
                {
                    referenceName = subscriptionId,
                    type = "CollectionReference"
                }
            };
            content = RequestContent.Create(serializable: collectionDetails);
            response = collectionClient.CreateOrUpdateCollection(content: content);
            log.LogInformation($"Purview Collection creation response {response}");
        }

        /// <summary>
        /// Creates a Storage account data source in a Purview collection.
        /// </summary>
        /// <param name="resourceId">Resource ID of the Storage Account.</param>
        /// <param name="subscriptionId">Subscription ID of the Storage Account.</param>
        /// <param name="resourceGroupName">Resource Group Name of the storage Account.</param>
        /// <param name="resourceName">Name of the Storage Account.</param>
        /// <param name="purviewScanEndpoint">Scan Endpoint of the Purview account.</param>
        /// <param name="log">Logger object to capture logs.</param>
        /// <remarks>
        /// Onboards a Storage Account to the resource group Purview Collection.
        /// </remarks>
        private static void CreateStorageAccount(string resourceId, string subscriptionId, string resourceGroupName, string resourceName, string purviewScanEndpoint, ILogger log)
        {
            // Get storage account details
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var armClient = new ArmClient(credential: credential);
            var resourceGroup = armClient.GetResourceGroup(id: new ResourceIdentifier(resourceId: $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}"));
            var storageAccounts = resourceGroup.GetStorageAccounts();
            var storageAccount = storageAccounts.Get(accountName: resourceName);

            // Create Purview Data Source Client
            var endpoint = new Uri(uriString: purviewScanEndpoint);
            var dataSourceClient = new PurviewDataSourceClient(endpoint: endpoint, dataSourceName: resourceName, credential: credential);

            // Create a Data Source
            var dataSourceDetails = new
            {
                name = resourceName,
                kind = storageAccount.Value.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2" : "AzureStorage",
                properties = new
                {
                    resourceId = resourceId,
                    subscriptionId = subscriptionId,
                    resourceGroup = resourceGroupName,
                    resourceName = resourceName,
                    endpoint = storageAccount.Value.Data.IsHnsEnabled.Equals(true) ? $"https://{resourceName}.dfs.core.windows.net/" : $"https://{resourceName}.blob.core.windows.net/",
                    location = storageAccount.Value.Data.Location.ToString(),
                    collection = new
                    {
                        referenceName = resourceGroupName,
                        type = "CollectionReference"
                    }
                }
            };
            var content = RequestContent.Create(serializable: dataSourceDetails);
            var response = dataSourceClient.CreateOrUpdate(content: content);
            log.LogInformation($"Purview Data Source creation response: '{response}'");

            // Create a Purview Scan Client
            var scanClient = new PurviewScanClient(endpoint: endpoint, dataSourceName: resourceName, scanName: "default", credential: credential);

            // Create a Scan
            var scanDetails = new
            {
                name = "default",
                kind = storageAccount.Value.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2Msi" : "AzureStorageMsi",
                properties = new
                {
                    scanRulesetName = storageAccount.Value.Data.IsHnsEnabled.Equals(true) ? "AdlsGen2" : "AzureStorage",
                    scanRulesetType = "System",
                    collection = new
                    {
                        referenceName = resourceGroupName,
                        type = "CollectionReference"
                    }
                }
            };
            content = RequestContent.Create(serializable: scanDetails);
            response = scanClient.CreateOrUpdate(content: content);
            log.LogInformation($"Purview Scan creation response: '{response}'");

            // Create a Trigger
            var triggerDetails = new
            {
                name = "default",
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
            content = RequestContent.Create(serializable: triggerDetails);
            response = scanClient.CreateOrUpdateTrigger(content: content);
            log.LogInformation($"Purview Scan Trigger creation response: '{response}'");

            // Create a Filter
            var filterDetails = new
            {
                properties = new
                {
                    excludeUriPrefixes = new string[] { },
                    includeUriPrefixes = new string[] { storageAccount.Value.Data.IsHnsEnabled.Equals(true) ? $"https://{resourceName}.dfs.core.windows.net/" : $"https://{resourceName}.blob.core.windows.net" }
                }
            };
            content = RequestContent.Create(serializable: filterDetails);
            response = scanClient.CreateOrUpdateFilter(content: content);
            log.LogInformation($"Purview Filter creation response: '{response}'");

            // Run Scan
            var options = new Azure.RequestOptions();
            var guid = Guid.NewGuid().ToString();
            response = scanClient.RunScan(runId: guid, options: options, scanLevel: "Full");
            log.LogInformation($"Purview Scan creation response: '{response}'");
        }

        /// <summary>
        /// Creates a Synapse workspace data source in a Purview collection.
        /// </summary>
        /// <param name="resourceId">Resource ID of the Storage Account.</param>
        /// <param name="subscriptionId">Subscription ID of the Storage Account.</param>
        /// <param name="resourceGroupName">Resource Group Name of the storage Account.</param>
        /// <param name="resourceName">Name of the Storage Account.</param>
        /// <param name="purviewScanEndpoint">Scan Endpoint of the Purview account.</param>
        /// <param name="log">Logger object to capture logs.</param>
        /// <remarks>
        /// Onboards a Synapse Workspace to the resource group Purview Collection.
        /// </remarks>
        private static void CreateSynapseWorkspace(string resourceId, string subscriptionId, string resourceGroupName, string resourceName, string purviewScanEndpoint, string purviewAccountName, string purviewResourceId, string purviewManagedStorageId, string purviewManagedEventHubId, ILogger log)
        {
            // Get synapse workspace details
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var armClient = new ArmClient(credential: credential);
            var resourceGroup = armClient.GetResourceGroup(id: new ResourceIdentifier(resourceId: $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}"));
            var synapse = armClient.GetGenericResource(id: new ResourceIdentifier(resourceId: resourceId)).Get();

            // Create Purview Data Source Client
            var endpoint = new Uri(uriString: purviewScanEndpoint);
            var dataSourceClient = new PurviewDataSourceClient(endpoint: endpoint, dataSourceName: resourceName, credential: credential);

            // Create a Data Source
            var dataSourceDetails = new
            {
                name = resourceName,
                kind = "AzureSynapseWorkspace",
                properties = new
                {
                    resourceId = resourceId,
                    subscriptionId = subscriptionId,
                    resourceGroup = resourceGroupName,
                    resourceName = resourceName,
                    serverlessSqlEndpoint = $"{resourceName}-ondemand.sql.azuresynapse.net",
                    dedicatedSqlEndpoint = $"{resourceName}.sql.azuresynapse.net",
                    location = synapse.Value.Data.Location.ToString(),
                    collection = new
                    {
                        referenceName = resourceGroupName,
                        type = "CollectionReference"
                    }
                }
            };
            var content = RequestContent.Create(serializable: dataSourceDetails);
            var response = dataSourceClient.CreateOrUpdate(content: content);
            log.LogInformation($"Purview Data Source creation response: '{response}'");

            // Create Synapse role assigment client
            endpoint = new Uri(uriString: $"https://{resourceName}.dev.azuresynapse.net");

            // Create Synapse role assignment
            var functionPrincipalId = GetEnvironmentVariable(name: "FunctionPrincipalId");
            var roleAssignmentsClient = new RoleAssignmentsClient(endpoint: endpoint, credential: credential);
            var roleAssignmentResponse = roleAssignmentsClient.CreateRoleAssignment(roleAssignmentId: functionPrincipalId, roleId: new Guid("dd665582-e433-40ca-b183-1b1b33e73375"), principalId: new Guid(functionPrincipalId), scope: $"workspaces/{resourceName}");
            log.LogInformation($"Purview role assigment response: '{roleAssignmentResponse}'");

            // Create Synapse managed private endpoints client
            var managedPrivateEndpointsClient = new ManagedPrivateEndpointsClient(endpoint: endpoint, credential: credential);

            // Create managed private endpoints on managed vnet for Purview
            var privateEndpointDetails = new List<ManagedPrivateEndpointDetails>
            {
                new ManagedPrivateEndpointDetails{ Name = "Purview", GroupId = "account", ResourceId = purviewResourceId },
                new ManagedPrivateEndpointDetails{ Name = "Purview_blob", GroupId = "blob", ResourceId = purviewManagedStorageId },
                new ManagedPrivateEndpointDetails{ Name = "Purview_queue", GroupId = "queue", ResourceId = purviewManagedStorageId },
                new ManagedPrivateEndpointDetails{ Name = "Purview_namespace", GroupId = "namespace", ResourceId = purviewManagedEventHubId }
            };
            foreach (var privateEndpointDetail in privateEndpointDetails)
            {
                try
                {
                    managedPrivateEndpointsClient.Create(
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
                    log.LogError(exception: ex, message: $"Private endpoint creation failed: {privateEndpointDetail}");
                }
            }

            // Create Purview role assignment for Lineage
            var purviewRootCollectionMetadataPolicyId = GetEnvironmentVariable(name: "PurviewRootCollectionMetadataPolicyId");
            var pincipalId = synapse.Value.Data.Identity.SystemAssignedIdentity.PrincipalId.ToString();
            CreatePurviewRoleAssignment(purviewAccountName: purviewAccountName, purviewRootCollectionName: purviewAccountName, purviewRootCollectionMetadataPolicyId: purviewRootCollectionMetadataPolicyId, pincipalId: pincipalId);

            // Add the Purview account MSI on the serverless SQL databases - blocked because of https://github.com/MicrosoftDocs/sql-docs/issues/2323
            //try
            //{
            //    using var connection = new SqlConnection(connectionString: $"Server=tcp:{resourceName}-ondemand.sql.azuresynapse.net,1433;Initial Catalog=master;Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=\"Active Directory Default\";");
            //    connection.Open();

            //    using var sqlCommand = new SqlCommand(cmdText: $"CREATE LOGIN [{purviewAccountName}] FROM EXTERNAL PROVIDER;", connection: connection);
            //    sqlCommand.ExecuteNonQuery();
            //}
            //catch (Exception ex)
            //{
            //    log.LogError(exception: ex, message: "Failed to add Purview MSI to SQL serverless databases");
            //}
        }

        /// <summary>
        /// Deletes a data source in Purview.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="purviewScanEndpoint">Scan Endpoint of the Purview account.</param>
        /// <param name="log">Logger object to capture logs.</param>
        private static void DeleteDataSource(string resourceName, string purviewScanEndpoint, ILogger log)
        {
            // Create Purview Data Source Client
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var endpoint = new Uri(uriString: purviewScanEndpoint);
            var dataSourceClient = new PurviewDataSourceClient(endpoint: endpoint, dataSourceName: resourceName, credential: credential);

            // Delete a Data Source
            var response = dataSourceClient.Delete();
            log.LogInformation($"Purview Data Source deletion response: '{response}'");
        }

        private static void CreatePurviewRoleAssignment(string purviewAccountName, string purviewRootCollectionName, string purviewRootCollectionMetadataPolicyId, string pincipalId)
        {
            // Create Purview role client
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var endpoint = new Uri(uriString: $"https://{purviewAccountName}.purview.azure.com");
            var client = new PurviewMetadataPolicyClient(endpoint: endpoint, collectionName: purviewRootCollectionName, credential: credential);

            // Get Purview Metadata Policy
            var metadataPolicy = client.GetMetadataPolicy(policyId: purviewRootCollectionMetadataPolicyId, options: new());

            // Convert metadata policy to object
            JsonElement metadataPolicyJson = JsonDocument.Parse(GetContentFromResponse(metadataPolicy)).RootElement;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var metadataPolicyObject = JsonSerializer.Deserialize<PurviewMetadataPolicy>(element: metadataPolicyJson, options: options);

            // Add principal Id
            foreach (var attributerule in metadataPolicyObject.Properties.AttributeRules)
            {
                if (attributerule.Id.StartsWith("purviewmetadatarole_builtin_data-curator:")) // TOD Support different roles
                {
                    foreach (var dnfCondition in attributerule.DnfCondition[0])
                    {
                        if (dnfCondition.AttributeName.Equals("principal.microsoft.id"))
                        {
                            dnfCondition.AttributeValueIncludedIn?.Add(pincipalId);
                        }
                    }
                }
            }

            // Create role assignment
            var content = RequestContent.Create(metadataPolicyObject);
            var myResponse = client.UpdateMetadataPolicy(policyId: purviewRootCollectionMetadataPolicyId, content: content);
        }

        /// <summary>
        /// Returns an environment variable as string.
        /// </summary>
        /// <param name="name">Name of the environment variable.</param>
        /// <returns>Value of the environment variable as string.</returns>
        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static BinaryData GetContentFromResponse(Response r)
        {
            // Workaround azure/azure-sdk-for-net#21048, which prevents .Content from working when dealing with responses
            // from the playback system.

            MemoryStream ms = new MemoryStream();
            r.ContentStream.CopyTo(ms);
            return new BinaryData(ms.ToArray());
        }
    }
}
