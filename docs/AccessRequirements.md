# Access Requirements

The Purview event-driven automation will require few Azure RBAC role assignments for it to work. The tables below will summarize the access requirements. The role assignments are defined in the Infrastructure as Code (IaC) Bicep definitions and will be setup through the CI/CD pipeline or Deploy-to-Azure button.

## Deployment

For the deployment, the following role assignments are required within the subscription where the Azure Function gets deployed as well as all the subscriptions listed in the `eventGridTopicSourceSubscriptions` input parameter:

| Service           | Role Assignment | Scope | Reason |
|:------------------|:----------------|:------|:-------|
| Service Principal | Contributor     | `/subscriptions/{subscriptionId}` | The service principal creates a number of resource groups as well as resources for the Function deployment in one subscription and a resource group with an Event Grid System Topic in each subscription mentioned in the `eventGridTopicSourceSubscriptions` input parameter. |
| Service Principal | User Access Administrator | `/subscriptions/{subscriptionId}` | The service principal creates few role assignments for the Azure Function to work properly. In addition, it will assign the Function few roles in each subscription mentioned in the `eventGridTopicSourceSubscriptions` input parameter for it to be able to onboard data sources, setup scanning as well as lineage. More details can be found in the tables below. |

## Data Source Onboarding

For the data source onboarding, the following role assignments in each subscription listed in the `eventGridTopicSourceSubscriptions` input parameter are required for the Azure Function:

| Service        | Role Assignment | Scope | Reason |
|:---------------|:----------------|:------|:-------|
| Azure Function | Reader          | `/subscriptions/{subscriptionId}` | The Azure Function needs to be able to read properties of the resources that it onboards to Purview. For example, Purview requires details like the location of the resource as well as other resource specific parameters ofr the onboarding to complete successfully. |
| Azure Function | Purview Root Collection Admin (Purview Data Plane) | Root Collection in Purview | The Azure Function will need these access rights to create the Collection structure automatically and to onboard Data Sources within the newly created Collections. |

## Managed Private Endpoints

For the managed private endpoints, the same role assignments as for the [data source onboarding](#data-source-onboarding) are required. No additional role assignments required for this feature.

You can disable the setup of managed private endpoints in the [application settings](/docs/ApplicationSettings.md).

## Scanning

For the automated setup of scanning, the role assignments mentioned in [data source onboarding](#data-source-onboarding) are required. The following additional role assignments in each subscription listed in the `eventGridTopicSourceSubscriptions` input parameter are required for the Azure Function:

| Service        | Role Assignment | Scope | Reason |
|:---------------|:----------------|:------|:-------|
| Azure Function | Contributor     | `/subscriptions/{subscriptionId}` | The function needs Contributor rights in order to create the necessary link between Purview and the data services. For example, for Kusto the Function needs to deploy a resource of type `/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Kusto/clusters/{kustoClusterName}/principalAssignments/{guid}` so that Purview can read the contents of the databases. |
| Microsoft Purview | Reader          | `/subscriptions/{subscriptionId}` | Microsoft Purview requires Reader rights for many connectots to properly scan the data source. For more details, please review the [connector requirements specified in Microsoft Learn](https://learn.microsoft.com/en-us/azure/purview/register-scan-azure-multiple-sources). |
| Microsoft Purview | Storage Blob Data Reader | `/subscriptions/{subscriptionId}` | Microsoft Purview requires Storage Data Reader rights to scan any kind of storage account. |

You can disable the setup of scans in the [application settings](/docs/ApplicationSettings.md). Once the scanning functionality is disabled, you can remove the role assignments and disable the deployment of them in the Bicep templates.

## Lineage

For the automated setup of lineage, the role assignments mentioned in the [data source onboarding](#data-source-onboarding) section and the role assignments mentioned in the [scanning](#scanning) section are required. The following additional role assignments in each subscription listed in the `eventGridTopicSourceSubscriptions` input parameter are required for the Azure Function:

| Service        | Role Assignment | Scope | Reason |
|:---------------|:----------------|:------|:-------|
| Azure Function | User Access Administrator | `/subscriptions/{subscriptionId}` | The function needs User Access Administrator Rights to be able to assign itself rights to a newly created Synapse workspace or Data Factory. The access to the Synapse workspace is required to create managed private endpoints to the Purview account on the Synapse managed virtual network. |

You can disable the setup of lineage in the [application settings](/docs/ApplicationSettings.md). Once the lineage functionality is disabled, you can remove the role assignments and disable the deployment of them in the Bicep templates.
