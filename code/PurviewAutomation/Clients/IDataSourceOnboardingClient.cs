using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal interface IDataSourceOnboardingClient
{
    internal Task AddDataSourceAsync();
    internal Task RemoveDataSourceAsync();
    internal Task AddScanAsync(bool triggerScan);
    internal Task AddManagedPrivateEndpointAsync();
    internal Task OnboardDataSourceAsync(bool setupScan = true, bool triggerScan = true);
}
