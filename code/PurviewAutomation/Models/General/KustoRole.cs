namespace PurviewAutomation.Models.General;

internal enum KustoRole
{
    AllDatabasesAdmin,
    AllDatabasesViewer,
    AllDatabasesMonitor
}

internal static class KustoRoleConverter
{
    internal static string ConvertRoleToString(KustoRole role)
    {
        return role switch
        {
            KustoRole.AllDatabasesAdmin => "AllDatabasesAdmin",
            KustoRole.AllDatabasesViewer => "AllDatabasesViewer",
            KustoRole.AllDatabasesMonitor => "AllDatabasesMonitor",
            _ => null,
        };
    }
}
