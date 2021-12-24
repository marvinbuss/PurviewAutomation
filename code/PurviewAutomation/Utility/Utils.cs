using Azure;
using System;
using System.IO;

namespace PurviewAutomation.Utility;

internal static class Utils
{
    internal static BinaryData GetContentFromResponse(Response r)
    {
        // Workaround azure/azure-sdk-for-net#21048, which prevents .Content from working when dealing with responses
        // from the playback system.
        var ms = new MemoryStream();
        r.ContentStream.CopyTo(ms);
        return new BinaryData(ms.ToArray());
    }
}
