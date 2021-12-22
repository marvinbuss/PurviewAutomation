using System;

namespace PurviewAutomation.Models.General;

internal enum SynapseRole
{
    Administrator,
    SqlAdministrator,
    ApacheSparkAdministrator,
    Contributor,
    ArtifactPublisher,
    ArtifactUser,
    ComputeOperator,
    CredentialUser,
    LinkedDataManager,
    User
}

internal static class SynapseRoleConverter
{
    internal static Guid ConvertRoleToGuid(SynapseRole role)
    {
        return role switch
        {
            SynapseRole.Administrator => new Guid("6e4bf58a-b8e1-4cc3-bbf9-d73143322b78"),
            SynapseRole.SqlAdministrator => new Guid("7af0c69a-a548-47d6-aea3-d00e69bd83aa"),
            SynapseRole.ApacheSparkAdministrator => new Guid("c3a6d2f1-a26f-4810-9b0f-591308d5cbf1"),
            SynapseRole.Contributor => new Guid("7572bffe-f453-4b66-912a-46cc5ef38fda"),
            SynapseRole.ArtifactPublisher => new Guid("05930f57-09a3-4c0d-9fa9-6d1eb91c178b"),
            SynapseRole.ArtifactUser => new Guid("53faaa0e-40b6-40c8-a2ff-e38f2d388875"),
            SynapseRole.ComputeOperator => new Guid("e3844cc7-4670-42cb-9349-9bdac1ee7881"),
            SynapseRole.CredentialUser => new Guid("5eb298b4-692c-4241-9cf0-f58a3b42bb25"),
            SynapseRole.LinkedDataManager => new Guid("dd665582-e433-40ca-b183-1b1b33e73375"),
            SynapseRole.User => new Guid("2a385764-43e8-416c-9825-7b18d05a2c4b"),
            _ => throw new ArgumentException(message: "Incorrect role provided", paramName: nameof(role)),
        };
    }
}
