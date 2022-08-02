using Azure.ResourceManager.CosmosDB;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal interface IDataSourceOnboardingClient
{
    internal Task AddDataSourceAsync();
    internal Task RemoveDataSourceAsync();
    internal Task AddScanAsync(bool triggerScan, string managedIntegrationRuntimeName);
    internal Task<string> AddScanningManagedPrivateEndpointsAsync();
    internal Task OnboardDataSourceAsync(bool useManagedPrivateEndpoints = true, bool setupScan = true, bool triggerScan = true);
}
