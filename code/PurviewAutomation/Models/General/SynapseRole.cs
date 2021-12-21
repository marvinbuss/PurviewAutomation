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
            SynapseRole.Administrator => new Guid(""),
            SynapseRole.SqlAdministrator => new Guid(""),
            SynapseRole.ApacheSparkAdministrator => new Guid(""),
            SynapseRole.Contributor => new Guid(""),
            SynapseRole.ArtifactPublisher => new Guid(""),
            SynapseRole.ArtifactUser => new Guid(""),
            SynapseRole.ComputeOperator => new Guid(""),
            SynapseRole.CredentialUser => new Guid(""),
            SynapseRole.LinkedDataManager => new Guid(""),
            SynapseRole.User => new Guid(""),
            _ => throw new ArgumentException(message: "Incorrect role provided", paramName: nameof(role)),
        };
    }
}
