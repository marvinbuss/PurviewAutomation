namespace PurviewAutomation.Models.Purview;

internal enum PurviewRole
{
    CollectionAdministrator,
    DataSourceAdministrator,
    DataCurator,
    DataReader,
    PolicyAuthor
}

internal static class PurviewRoleConverter
{
    internal static string ConvertRoleToString(PurviewRole role)
    {
        return role switch
        {
            PurviewRole.CollectionAdministrator => "purviewmetadatarole_builtin_collection-administrator",
            PurviewRole.DataSourceAdministrator => "purviewmetadatarole_builtin_data-source-administrator",
            PurviewRole.DataCurator => "purviewmetadatarole_builtin_data-curator",
            PurviewRole.DataReader => "purviewmetadatarole_builtin_purview-reader",
            PurviewRole.PolicyAuthor => "purviewmetadatarole_builtin_policy-author",
            _ => null,
        };
    }
}
