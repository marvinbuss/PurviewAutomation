using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal interface ILineageOnboardingClient
{
    internal Task AddManagedPrivateEndpointsAsync();
    internal Task OnboardLineageAsync(string principalId);
}
