# Microsoft Purview Event-driven Automation

---

_This solution will simplify data governance tasks in an organization that uses Microsoft Purview as its core data catalog._

---

In a decentralized Data Platform like the [Data Management & Analytics Scenario](https://github.com/Azure/data-management-zone) reference architecture, it becomes increasingly difficult for data governance personas to govern the data estate. Data Product teams can create their data services in a self-service way, which makes it increasingly difficult for data governance personas to onboard data sources, scan the content, classify the data and govern the quality of the data. This solution aims at automating these tasks.

The following automation is being taken care of by this solution:

1. Discovery of Data Sources within the Data Platform.
2. Onboarding of Data Sources within the respective Microsoft Purview Collection.
3. Setup of Managed Private Endpoints onto the managed virtual network inside Purview.
4. Setup of Scanning and Triggers for Data Sources.
5. Setup of Lineage for supported Data Sources (Synapse, Data Factory, etc.).

Below you will find some of the core capabilities:

## Event-driven onboarding of Data Sources

The solution onboards data sources automatically to a Microsoft Purview collection when a new data source gets created within an Azure subscription. The a collection structure will be automatically created within your Purview account.

![Event-driven onboarding of Data Sources](/docs/images/PurviewOnboarding.gif)

## Event-driven removal of Data Sources

The solution can remove data sources when they get deleted in Azure. Scaned data assets will not get removed when data sources get deleted from a Purview collection. This feature can be disabled.

![Event-driven removal of Data Sources](/docs/images/PurviewRemoval.gif)

## Automated setup of Scans

The solution can automatically create and trigger scans for data services. This feature can be disabled.

![Automated setup of Scans](/docs/images/PurviewScanning.gif)

## Deployment

You have two options, to deploy this solution to your Azure tenant:

1. GitHub Actions and
2. Deploy to Azure Button.

[![Deploy To Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#blade/Microsoft_Azure_CreateUIDef/CustomDeploymentBlade/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmarvinbuss%2FPurviewAutomation%2Fmain%2Finfra%2Fmain.json/uiFormDefinitionUri/https%3A%2F%2Fraw.githubusercontent.com%2Fmarvinbuss%2FPurviewAutomation%2Fmain%2Fdocs%2Freference%2Fportal.json)

For more details, [please visit the deployment documentation page](/docs/Deployment.md).

## More Details

1. [Deployment](/docs/Deployment.md)
2. [Supported Services](/docs/SupportedServices.md)
3. [Access Requirements](/docs/AccessRequirements.md)
4. [Default Collection Structure](/docs/DefaultCollectionStructure.md)
5. [Application Settings](/docs/ApplicationSettings.md)
6. [Architecture](/docs/Architecture.md)
