using System.Collections.Generic;

namespace PurviewAutomation.Models;

/// <summary>
/// Record struct for specifying managed private endpoint details.
/// </summary>
internal record struct ManagedPrivateEndpointDetails
{
    public string Name { get; init; }
    public string ResourceId { get; init; }
    public string GroupId { get; init; }
}