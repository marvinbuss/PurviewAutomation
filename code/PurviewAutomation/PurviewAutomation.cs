// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using PurviewAutomation.Clients;
using PurviewAutomation.Models.General;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PurviewAutomation;

public static class PurviewAutomation
{
    [FunctionName("PurviewAutomation")]
    public static async Task RunAsync([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
    {
        log.LogInformation("Parsing Event Grid event data");
        var eventDetails = GetEventDetails(eventGridEvent: eventGridEvent);

        // Check event status
        if (eventDetails.Status != "succeeded")
        {
            log.LogError($"Received Event Grid event from a non-succeeded operation (State: {eventDetails.Status})");
            throw new Exception("Received Event Grid event from a non-succeeded operation");
        }

        // Crate Purview automation client
        var purviewAutomationClient = new PurviewAutomationClient(
            resourceId: GetEnvironmentVariable(name: "PurviewResourceId"),
            managedStorageResourceId: GetEnvironmentVariable(name: "PurviewManagedStorageId"),
            managedEventHubId: GetEnvironmentVariable(name: "PurviewManagedEventHubId"),
            rootCollectionName: GetEnvironmentVariable(name: "PurviewRootCollectionName"),
            rootCollectionPolicyId: GetEnvironmentVariable(name: "PurviewRootCollectionMetadataPolicyId"),
            logger: log);

        // Get application settings
        var functionPrincipalId = GetEnvironmentVariable(name: "FunctionPrincipalId");
        var setupScan = Convert.ToBoolean(GetEnvironmentVariable(name: "SetupScan"));
        var triggerScan = Convert.ToBoolean(GetEnvironmentVariable(name: "TriggerScan"));
        var setupLineage = Convert.ToBoolean(GetEnvironmentVariable(name: "SetupLineage"));
        var removeDataSources = Convert.ToBoolean(GetEnvironmentVariable(name: "RemoveDataSources"));

        switch (eventDetails.Operation)
        {
            case "write":
                log.LogInformation($"Write opreation detected");
                await AddDataSourceAsync(eventDetails: eventDetails, purviewAutomationClient: purviewAutomationClient, setupScan: setupScan, triggerScan: triggerScan, setupLineage: setupLineage, functionPrincipalId: functionPrincipalId, logger: log);
                break;
            case "delete":
                log.LogInformation($"Delete operation detected");
                if (removeDataSources)
                {
                    log.LogInformation($"Removing data source");
                    await RemoveDataSourceAsync(eventDetails: eventDetails, purviewAutomationClient: purviewAutomationClient, logger: log);
                }
                else
                {
                    log.LogInformation($"NOT removing data source because the feature is turned off");
                }
                break;
            default:
                log.LogInformation($"Unsupported operation detected: '{eventDetails.Operation}'");
                break;
        }
    }

    /// <summary>
    /// Parses the Event Grid Event
    /// </summary>
    /// <param name="eventGridEvent">Event Grid Event that triggered the function.</param>
    /// <returns>Event details.</returns>
    private static EventDetails GetEventDetails(EventGridEvent eventGridEvent)
    {
        // Parse 
        var eventGridEventJsonObject = JsonNode.Parse(eventGridEvent.Data.ToString());

        return new EventDetails
        {
            Status = eventGridEventJsonObject["status"].ToString().ToLower(),
            Action = eventGridEventJsonObject["authorization"]["action"].ToString().ToLower(),
            Operation = eventGridEventJsonObject["authorization"]["action"].ToString().Split(separator: "/")[^1].ToLower(),
            Scope = eventGridEventJsonObject["authorization"]["scope"].ToString()
        };
    }

    /// <summary>
    /// Adds the supported data source to the Purview account.
    /// </summary>
    /// <param name="eventDetails">Object containing the event details.</param>
    /// <param name="purviewAutomationClient">Client for Purview interactionss.</param>
    /// <param name="setupScan">Specifies whether scans should be setup.</param>
    /// <param name="triggerScan">Specifies whether the initial scan should be triggered.</param>
    /// <param name="setupLineage">Specifies whether lineage should be setup.</param>
    /// <param name="functionPrincipalId">Principal ID of the function.</param>
    /// <param name="logger">Object for logging.</param>
    /// <returns></returns>
    private static async Task AddDataSourceAsync(EventDetails eventDetails, PurviewAutomationClient purviewAutomationClient, bool setupScan, bool triggerScan, bool setupLineage, string functionPrincipalId, ILogger logger)
    {
        if (eventDetails.Action == "microsoft.storage/storageaccounts/write")
        {
            logger.LogInformation("Storage Account creation detected");
            var storageOnboardingClient = new StorageOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await storageOnboardingClient.OnboardDataSourceAsync(setupScan: setupScan, triggerScan: triggerScan);
        }
        else if (eventDetails.Action == "microsoft.synapse/workspaces/write")
        {
            logger.LogInformation("Synapse Workspace creation detected");
            var synapseOnboardingClient = new SynapseOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await synapseOnboardingClient.OnboardDataSourceAsync(setupScan: setupScan, triggerScan: triggerScan);

            if (setupLineage)
            {
                await synapseOnboardingClient.OnboardLineageAsync(principalId: functionPrincipalId);
            }
        }
        else if (eventDetails.Action == "microsoft.kusto/clusters/write")
        {
            logger.LogInformation("Kusto Cluster creation detected");
            var kustoOnboardingClient = new KustoOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await kustoOnboardingClient.OnboardDataSourceAsync(setupScan: setupScan, triggerScan: triggerScan);
        }
        else if (eventDetails.Action == "microsoft.documentdb/databaseaccounts/write")
        {
            logger.LogInformation("Cosmos DB creation detected");
            var cosmosOnboardingClient = new CosmosOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await cosmosOnboardingClient.OnboardDataSourceAsync(setupScan: setupScan, triggerScan: triggerScan);
        }
        else if (eventDetails.Action == "microsoft.sql/servers/write")
        {
            logger.LogInformation("SQL Server creation detected");
            var sqlServerOnboardingClient = new SqlServerOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await sqlServerOnboardingClient.OnboardDataSourceAsync(setupScan: setupScan, triggerScan: triggerScan);
        }
        else if (eventDetails.Action == "microsoft.sql/servers/databases/write")
        {
            logger.LogInformation("SQL Database creation detected");
            var sqlDatabaseOnboardingClient = new SqlDatabaseOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await sqlDatabaseOnboardingClient.OnboardDataSourceAsync(setupScan: setupScan, triggerScan: triggerScan);
        }
        else
        {
            logger.LogInformation($"Unsupported resource creation detected: {eventDetails.Action}");
        }
    }

    /// <summary>
    /// Removes the supported data source from the Purview account.
    /// </summary>
    /// <param name="eventDetails">Object containing the event details.</param>
    /// <param name="purviewAutomationClient">Client for Purview interactionss.</param>
    /// <param name="logger">Object for logging.</param>
    /// <returns></returns>
    private static async Task RemoveDataSourceAsync(EventDetails eventDetails, PurviewAutomationClient purviewAutomationClient, ILogger logger)
    {
        if (eventDetails.Action == "microsoft.storage/storageaccounts/delete")
        {
            logger.LogInformation("Storage Account deletion detected");
            var storageOnboardingClient = new StorageOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await storageOnboardingClient.RemoveDataSourceAsync();
        }
        else if (eventDetails.Action == "microsoft.synapse/workspaces/delete")
        {
            logger.LogInformation("Synapse Workspace deletion detected");
            var synapseOnboardingClient = new SynapseOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await synapseOnboardingClient.RemoveDataSourceAsync();
        }
        else if (eventDetails.Action == "microsoft.kusto/clusters/delete")
        {
            logger.LogInformation("Kusto Cluster deletion detected");
            var kustoOnboardingClient = new KustoOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await kustoOnboardingClient.RemoveDataSourceAsync();
        }
        else if (eventDetails.Action == "microsoft.documentdb/databaseaccounts/delete")
        {
            logger.LogInformation("Cosmos DB deletion detected");
            var cosmosOnboardingClient = new CosmosOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await cosmosOnboardingClient.RemoveDataSourceAsync();
        }
        else if (eventDetails.Action == "microsoft.sql/servers/delete")
        {
            logger.LogInformation("SQL Server deletion detected");
            var sqlServerOnboardingClient = new SqlServerOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await sqlServerOnboardingClient.RemoveDataSourceAsync();
        }
        else if (eventDetails.Action == "microsoft.sql/servers/databases/delete")
        {
            logger.LogInformation("SQL Database deletion detected");
            var sqlDatabaseOnboardingClient = new SqlDatabaseOnboardingClient(resourceId: eventDetails.Scope, client: purviewAutomationClient, logger: logger);
            await sqlDatabaseOnboardingClient.RemoveDataSourceAsync();
        }
        else
        {
            logger.LogInformation($"Unsupported resource deletion detected: {eventDetails.Action}");
        }
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
