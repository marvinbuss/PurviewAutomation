# Purview Event-driven Automation

---

_This solution will simplify data governance tasks in an organization that uses Purview as its core data catalog._

---

In a decentralized Data Platform like the [Data Management & Analytics Scenario](https://github.com/Azure/data-management-zone) reference architecture, it becomes increasingly difficult for data governance personas to govern the data estate. Data Product teams can create their data services in a self-service way, which makes it increasingly difficult for data governance personas to onboard data sources, scan the content, classify the data and govern the quality of the data. This solution aims at automating these tasks.

The following automation is being taken care of by this solution:

1. Discovery of Data Sources within the Data Platform.
2. Onboarding of Data Sources within the respective Purview Collection.
3. Setup of Managed Private Endpoints onto the managed virtual network inside Purview.
4. Setup of Scanning and Triggers for Data Sources.
5. Setup of Lineage for supported Data Sources (Synapse, Data Factory, etc.).

Below you will find some of the core capabilities:

## Event-driven onboarding of Data Sources

The solution will onboard data sources based on events. In addition, a collection structure will automatically be created within your Purview account.



## More Details

1. [Supported Services](/docs/SupportedServices.md)
2. [Access Requirements](/docs/AccessRequirements.md)
3. [Default Collection Structure](/docs/DefaultCollectionStructure.md)
