using System.Threading.Tasks;

namespace PurviewAutomation.Clients;

internal interface ILineageOnboardingClient
{
    internal Task AddLineageManagedPrivateEndpointsAsync();
    internal Task OnboardLineageAsync(string principalId);
}
