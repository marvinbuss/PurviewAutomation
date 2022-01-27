# Purview Event-driven Automation

This solution aims to automate the following tasks within a Purview account:

1. Discovery of Data Sources within the Data Platform.
2. Onboarding of Data Sources within the respective Purview Collection.
3. Setup of Managed Private Endpoints onto the managed virtual network inside Purview.
4. Setup of Scanning and Triggers for Data Sources.
5. Setup of Lineage for supported Data Sources (Synapse, Data Factory, etc.).

## Supported Services

The table below provides and overview of the currently supported data sources and features:

| Data Source Name              | Data Source Onboarding | Managed Private Endpoint Setup | Scanning           | Lineage            |
|:------------------------------|:-----------------------|:-------------------------------|:-------------------|:-------------------|
| Azure Synapse Analytics       | :heavy_check_mark:     | :x:                            | :x:                | :heavy_check_mark: |
| Azure Blob Storage            | :heavy_check_mark:     | :x:                            | :heavy_check_mark: | N/A                |
| Azure Cosmos DB (SQL API)     | :heavy_check_mark:     | :x:                            | :x:                | N/A                |
| Azure Data Explorer (Kusto)   | :heavy_check_mark:     | :x:                            | :x:                | N/A                |
| Azure Data Lake Gen2          | :heavy_check_mark:     | :x:                            | :heavy_check_mark: | N/A                |
| Azure Database for MySQL      | :x:                    | :x:                            | :x:                | N/A                |
| Azure Database for PostgreSQL | :x:                    | :x:                            | :x:                | N/A                |
| Azure SQL Database            | :heavy_check_mark:     | :x:                            | :x:                | N/A                |
| Azure SQL Managed Instance    | :x:                    | :x:                            | :x:                | N/A                |
| Azure Data Factory            | :x:                    | :x:                            | N/A                | :x:                |
| Azure Data Share              | :x:                    | :x:                            | N/A                | :x:                |

The solution will primarily focus on Azure Data Sources today and not support any non-native services for the time being.

## Manual Steps after Deployment

After the deployment of this solution, you will have to add the Function MSI to the Purview Root Collection as Data Source Admin. This is required, so that the function can successfully  onboard data sources and lineage sources to Purview.
