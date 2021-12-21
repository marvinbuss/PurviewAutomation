namespace PurviewAutomation.Models.Purview;

/// <summary>
/// Record struct for specifying managed private endpoint details.
/// </summary>
internal record struct CollectionDetails
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string ParentCollectionName { get; init; }
}