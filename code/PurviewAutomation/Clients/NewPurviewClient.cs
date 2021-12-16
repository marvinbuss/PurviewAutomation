using System;

namespace PurviewAutomation.Clients;

internal class NewPurviewClient
{
    private readonly string purviewName;
    private readonly string purviewResourceId;
    private readonly string purviewManagedStorageResourceId;
    private readonly string purviewManagedEventHubId;
    private readonly string purviewAccountEndpoint;
    private readonly string purviewScanEndpoint;

    internal NewPurviewClient(string purviewResourceId, string purviewManagedStorageResourceId, string purviewManagedEventHubId)
    {
        if (purviewResourceId.Split(separator: "/").Length != 9 ||
            purviewManagedStorageResourceId.Split(separator: "/").Length != 9 ||
            purviewManagedEventHubId.Split(separator: "/").Length != 9)
        {
            throw new ArgumentException(message: "Incorrect Resource IDs provided", paramName: "purviewResourceId");
        }
        this.purviewName = purviewResourceId.Split(separator: "/")[8];
        this.purviewResourceId = purviewResourceId;
        this.purviewManagedStorageResourceId = purviewManagedStorageResourceId;
        this.purviewManagedEventHubId = purviewManagedEventHubId;
        this.purviewAccountEndpoint = $"https://{this.purviewName}.purview.azure.com/account";
        this.purviewScanEndpoint = $"https://{this.purviewName}.purview.azure.com/scan";
    }
}
