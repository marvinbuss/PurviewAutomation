namespace PurviewAutomation.Models.Purview
{
    internal enum Role
    {
        CollectionAdministrator,
        DataSourceAdministrator,
        DataCurator,
        DataReader,
        PolicyAuthor
    }

    internal static class RoleConverter
    {
        internal static string ConvertRoleToString(Role role)
        {
            return role switch
            {
                Role.CollectionAdministrator => "purviewmetadatarole_builtin_collection-administrator",
                Role.DataSourceAdministrator => "purviewmetadatarole_builtin_data-source-administrator",
                Role.DataCurator => "purviewmetadatarole_builtin_data-curator",
                Role.DataReader => "purviewmetadatarole_builtin_purview-reader",
                Role.PolicyAuthor => "purviewmetadatarole_builtin_policy-author",
                _ => null,
            };
        }
    }
}
