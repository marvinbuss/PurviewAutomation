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
using Azure.Analytics.Purview.Account;
using Azure.Analytics.Purview.Scanning;

namespace PurviewAutomation
{
    public static class PurviewAutomation
    {
        [FunctionName("PurviewAutomation")]
        public static void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation("Parsing Event Grid event data");
            var eventGridEventString = eventGridEvent.Data.ToString();
            var eventGridEventJsonObject = JsonNode.Parse(eventGridEventString);

            // Check if Event Grid event has state succeeded
            var eventGridEventStatus = eventGridEventJsonObject["status"].ToString().ToLower();
            if (eventGridEventStatus != "succeeded")
            {
                log.LogError("Received Event Grid event in a non succeeded state");
                throw new Exception("Received Event Grid event in a non succeeded state");
            }

            // Parsing Event Grid details
            var eventGridEventAction = eventGridEventJsonObject["authorization"]["action"].ToString();
            var eventGridEventScope = eventGridEventJsonObject["authorization"]["scope"].ToString();

            // Parsing scope details and get env variables
            var eventGridEventScopeArray = eventGridEventScope.Split(separator: "/");
            if (eventGridEventScopeArray.Length < 9)
            {
                log.LogError("Incorrect scope length");
                throw new Exception("Incorrect scope length");
            }
            var purviewAccountName = GetEnvironmentVariable(name: "PurviewAccountName");
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
                    log.LogInformation("Storage Account deployment detected");
                    PurviewCollectionSetup(subscriptionId: subscriptionId, resourceGroupName: resourceGroupName, purviewRootCollectionName: purviewRootCollectionName, purviewAccountEndpoint: purviewAccountEndpoint, log: log);
                    BlobStorageAccountOnboarding(resourceId: eventGridEventScope, subscriptionId: subscriptionId, resourceGroupName: resourceGroupName, resourceName: resourceName, purviewScanEndpoint: purviewScanEndpoint, log: log);
                    break;
                default:
                    log.LogInformation($"Unsupported Event Grid action detected: '{eventGridEventAction}'");
                    break;
            }
            
        }

        /// <summary> 
        /// Creates or updates collections below the provided Purview Root collection.
        /// </summary>
        /// <param name="scope">The scope of the resource for which the Function was triggered.</param>
        /// <param name="log">The logger object to capture logs.</param>
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

        private static void BlobStorageAccountOnboarding(string resourceId, string subscriptionId, string resourceGroupName, string resourceName, string purviewScanEndpoint, ILogger log)
        {
            // Create Purview Data Source Client
            var endpoint = new Uri(uriString: purviewScanEndpoint);
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var dataSourceClient = new PurviewDataSourceClient(endpoint: endpoint, dataSourceName: resourceName, credential: credential);

            // Create a Data Source
            var dataSourceDetails = new
            {
                name = resourceName,
                kind = "AzureStorage",
                properties = new
                {
                    resourceId = resourceId,
                    subscriptionId = subscriptionId,
                    resourceGroup = resourceGroupName,
                    resourceName = resourceName,
                    endpoint = $"https://{resourceName}.blob.core.windows.net/",
                    location = "northeurope",  // TODO: Get resource location
                    collection = new
                    {
                        referenceName = resourceGroupName,
                        type = "CollectionReference"
                    }
                }
            };
            var content = RequestContent.Create(serializable: dataSourceDetails);
            var response = dataSourceClient.CreateOrUpdate(content: content);
            log.LogInformation($"Purview Collection creation response {response}");

            // Create a Purview Scan Client
            var scanClient = new PurviewScanClient(endpoint: endpoint, dataSourceName: resourceName, scanName: "default", credential: credential);

            // Create a Scan
            var scanDetails = new
            {
                name = "default",
                kind = "AzureStorageMsi",
                properties = new
                {
                    scanRulesetName = "AzureStorage",
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
            log.LogInformation($"Purview Collection creation response {response}");

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
            content = RequestContent.Create(serializable: scanDetails);
            response = scanClient.CreateOrUpdateTrigger(content: content);
            log.LogInformation($"Purview Collection creation response {response}");

            // Create a Filter
            var filterDetails = new
            {
                properties = new
                {
                    excludeUriPrefixes = new string[] { },
                    includeUriPrefixes = new string[] { $"https://{resourceName}.blob.core.windows.net" }
                }
            };
            content = RequestContent.Create(serializable: filterDetails);
            response = scanClient.CreateOrUpdateFilter(content: content);
            log.LogInformation($"Purview Collection creation response {response}");

            // Run Scan
            var options = new Azure.RequestOptions();
            var guid = System.Guid.NewGuid().ToString();
            response = scanClient.RunScan(runId: guid, options: options, scanLevel: "Full");
            log.LogInformation($"Purview Collection creation response {response}");
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
    }
}
