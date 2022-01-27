# Access Requirements

The Purview event-driven automation will need require few Azure RBAC role assignments for it to work. The tables below will summarize the access requirements.
The role assignments are defined in the Infrastructure as Code (IaC) Bicep definitions and will be setup through the CI/CD pipeline.

## Deployment

For the deployment, the following role assignments are required:

| Service           | Role Assignment | Scope | Reason |
|:------------------|:----------------|:------|:-------|
| Service Principal | Contributor     | `/subscriptions/{subscriptionId}` | The service principal creates resources and resource groups and therfore requires contributor access rights to the subscription. |
| Service Principal | User Access Administrator | `/subscriptions/{subscriptionId}` | The service principal creates few role assignments for this solution to function as expected. |

## Data Source Onboarding

For the data source onboarding, the following role assignments are required:

| Service        | Role Assignment | Scope | Reason |
|:---------------|:----------------|:------|:-------|
| Azure Function | Contributor     | `/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/systemTopics/{eventGridSystemTopicName}` | The function needs to be able to subscribe and receive events from the Event Grid System Topic. |
| Azure Function | Purview Root Collection Admin (Purview Data Plane) | Root Collection in Purview | The service will need these access rights in order to be able to create the Collection structure automatically and in order to onboard Data Sources within the Collections. |

## Managed Private Endpoints

For the managed private endpoints, the same role assignments as for the [data source onboarding](#data-source-onboarding) are required. No additional role assignments required.

## Scanning

For the automated setup of scanning, the role assignments mentioned in [data source onboarding](#data-source-onboarding) are required. The following additional role assignments are required:

| Service        | Role Assignment | Scope | Reason |
|:---------------|:----------------|:------|:-------|
| Azure Function | Contributor     | `/subscriptions/{subscriptionId}` | The function needs Contributor rights in order to create the necessary link between Purview and the data services. For example, for Kusto the Function needs to deploy a resource of type `/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Kusto/clusters/{kustoClusterName}/principalAssignments/{guid}` so that Purview can read the contents of the databases. |

## Lineage

For the automated setup of lineage, the role assignments mentioned in [data source onboarding](#data-source-onboarding) and teh role assignments mentioned in [scanning](#scanning) are required. The following additional role assignments are required:

| Service        | Role Assignment | Scope | Reason |
|:---------------|:----------------|:------|:-------|
| Azure Function | User Access Administrator | `/subscriptions/{subscriptionId}` | The function needs User Access Administrator Rights to be able to assign itself rights to a newly created Synapse workspace. The access to the Synapse workspace is required to create managed private endpoints to the Purview account on the Synapse managed virtual network. |
