# Deployment Guide

Please follow the guide below to deploy the solution into your own tenant and subscription. 

## 0. Prerequisites

Before getting started with the deployment, please review the prerequisites.

### Supported regions

The solution supports the following Azure regions:

- (Asia Pacific) Australia East
- (Canada) Canada Central
- (US) East US
- (US) East US 2
- (Europe) North Europe
- (Europe) West Europe
- (more to follow over the next weeks as [Microsoft Purview expands the supported regions for managed Vnets](https://docs.microsoft.com/en-us/azure/purview/catalog-managed-vnet#supported-regions))

### Required for this solution

The following must be available to deploy the solution inside your Azure environment:

- An Azure subscription. If you don't have an Azure subscription, [create your Azure free account today](https://azure.microsoft.com/free/).
- [User Access Administrator](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#user-access-administrator) or [Owner](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#owner) access to the subscription to be able to create a service principal and role assignments for it.
- For the deployment, please choose one of the **Supported Regions**.
- It is also expected that a few resources have been deployed upfront. This includes:
    - A Vnet with two subnets, whereas one needs to have `privateEndpointNetworkPolicies` disabled and the other one needs to be delegated to `Microsoft.Web/serverFarms`.
    - An Azure Purview account in one of the supported regions.
    - Private DNS Zones for blob storage, file storage, and key vault.

### (Optional) Deployment of required services

If the services mentioend previously have not been deployed upfront, you can also find a [bicep template and parameter file here](/docs/reference/prerequisites), which you can use to get your environment ready for the deployment of the Purview Automation solution. To deploy the setup, please execute the following steps:

1. Open [/docs/reference/prerequisites/params.json](/docs/reference/prerequisites/params.json) and update at least the prefix value, which will be used as a prefix for all the resources that are getting deployed.
2. Now, open your terminal and navigate to the location of these templates.
3. Run either of the following commands to deploy the setup inside your subscription:

    **Azure CLI:**

    ```sh
    az deployment sub create \
        --location "northeurope" \
        --template-file "main.bicep" \
        --template-file "params.json"
    ```

    **Azure PowerShell:**

    ```powershell
    New-AzSubscriptionDeployment `
        -Location "northeurope" `
        -TemplateFile "main.bicep" `
        -TemplateParameterFile "params.json"
    ```

4. Take note of the outputs of this deployment. The deployment provides output for all parameters that are required for subsequent steps.

Now, we can start with the deployment of teh actual solution.

## 1. Fork the repository

First, you must fork this repository. To do so, please follow the steps below:

1. Log in to GitHub using your GitHub account.
2. On GitHub, navigate to the [main page of this repository](https://github.com/marvinbuss/PurviewAutomation).
3. In the top-right corner of the page, click **Fork**.

    ![Fork GitHub repository](/docs/images/ForkRepository.png)

4. Select a repository name and description and click **Create Fork**.

## 2. Create Service Principal

A service principal with *Contributor* and *User Access Administrator* rights on the subscription level needs to be generated for authentication and authorization from GitHub to your Azure subscription. This is required to deploy resources to your environment. Follow the steps below to create a Service Principle:

1. Go to the Azure Portal to find the ID of your subscription.
2. Start the Cloud Shell or open your Terminal and login to Azure using the following Azure CLI command: `az login`.
3. Set the Azure context using `az account set --subscription "<your-subscription-id>"`.
4. Execute the following command using Azure CLI to generate the required Service Principal credentials:

    **Azure CLI:**

    ```sh
    # Replace <your-service-principal-name> and <your-subscription-id> with your
    # Azure subscription id and any name for your service principal.
    az ad sp create-for-rbac \
        --name "<your-service-principal-name>" \
        --role "Contributor" \
        --scopes "/subscriptions/<your-subscription-id>" \
        --sdk-auth
    ```

    This will generate the following JSON output:

    ```json
    {
        "clientId": "<GUID>",
        "clientSecret": "<GUID>",
        "subscriptionId": "<GUID>",
        "tenantId": "<GUID>",
        (...)
    }
    ```

    > **Note:** Take note of the output. It will be required for the next steps.

5. Run the following command using Azure CLI or Azure PowerShell to assign the *User Access Administrator* role:

    **Azure CLI:**

    ```sh
    # Get Service Principal Object ID
    az ad sp list \
        --display-name "<your-service-principal-name>" \
        --query "[].{objectId:objectId}" \
        --output tsv

    # Assign the User Access Administrator role.
    az role assignment create \
        --assignee "<your-service-principle-object-id>" \
        --role "User Access Administrator" \
        --scopes "/subscription/<your-subscription-id>"
    ```

    **Azure PowerShell:**
    
    ```powershell
    # Get Service Principal Object ID
    $spObjectId = (Get-AzADServicePrincipal -DisplayName "<your-service-principal-name>").id

    # Assign the User Access Administrator role.
    New-AzRoleAssignment `
        -ObjectId $spObjectId `
        -RoleDefinitionName "User Access Administrator" `
        -Scope "/subscription/<your-subscription-id>"
    ```

## 3. Add secret to GitHub repository

In the previous step, you have created Service Principal credentials which will now be saved as repository secret to deploy the solution to your subsciption:

1. On GitHub, navigate to the main page of the repository.
2. Under your repository name, click on the **Settings** tab.
3. In the left sidebar, click **Secrets**.
4. Click **New repository secret**.
5. Type the name `AZURE_CREDENTIALS` for your secret in the Name input box.
6. Enter the JSON output from above as value for your secret.
    ```json
    {
        "clientId": "<GUID>",
        "clientSecret": "<GUID>",
        "subscriptionId": "<GUID>",
        "tenantId": "<GUID>",
        (...)
    }
    ```
7. Click **Add secret**.

## 4. Update parameters

In order to deploy the Infrastructure as Code (IaC) templates to the desired Azure subscription, you will need to modify some parameters in the forked repository. Therefore, **this step should not be skipped**. There are two files that require updates:

- `.github/workflows/deploy.yml` and
- `infra/params.json`.

Update these files in a separate branch and then merge via Pull Request to trigger the initial deployment.  Follow the steps below to successfully update the parameters:

1. Open [.github/workflows/deploy.yml](/.github/workflows/deploy.yml).
2. Update `AZURE_SUBSCRIPTION_ID` and `AZURE_LOCATION` in the environment variables section:

    ```yaml
    env:
        DOTNET_VERSION: "6.0.x"
        WORKING_DIRECTORY: "code/"
        AZURE_SUBSCRIPTION_ID: "4060c03e-0d2e-44b7-82a3-da9376fe50b2"  # Update to '<your-subscription_id>'
        AZURE_LOCATION: "northeurope"                                  # Update to '<your-azure-region>'
    ```

    | Parameter                     | Description  | Sample value |
    |:------------------------------|:-------------|:-------------|
    | **AZURE_SUBSCRIPTION_ID**     | Specifies the subscription ID where all the resources will be deployed | <div style="width: 36ch">`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`</div> |
    | **AZURE_LOCATION**            | Specifies the region where you want the resources to be deployed. Please check [Supported Regions](#supported-regions) | `northeurope` |

3. Commit the changes.
4. Open [infra/params.json](/infra/params.json).
5. Update the variable values in this file. More details for each parameter can be found below:

    | Parameter                                | Description  | Sample value |
    |:-----------------------------------------|:-------------|:-------------|
    | `location` | Specifies the location for all resources. | `northeurope` |
    | `environment` | Specifies the environment of the deployment. | `dev`, `tst` or `prd` |
    | `prefix` | Specifies the prefix for all resources created in this deployment. | `prefi` |
    | `tags` | Specifies the tags that you want to apply to all resources. | {`key`: `value`} |
    | `purviewId` | Specifies the Resource ID of the central Purview instance. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.Purview/accounts/<your-purview-name>` |
    | `purviewRootCollectionName` | Specifies the name of the root collection of the Purview account. By default, the name is equal to your Purview account. | `dmz-dev-purview001` |
    | `purviewRootCollectionMetadataPolicyId` | Specifies the root collection metadata policy id of the Purview account. | `e647bedc-2322-4380-bfc3-cacf504e3b2f` |
    | `purviewManagedStorageId` | Specifies the Resource ID of the managed storage of the central purview instance. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.Storage/storageAccounts/<your-storage-account-name>` |
    | `purviewManagedEventHubId` | Specifies the Resource ID of the managed event hub of the central purview instance. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.EventHub/namespaces/<your-eventhub-namespace-name>` |
    | `eventGridTopicSourceSubscriptions` | Specifies ... | `[{"subscriptionId": "<your-subscription-id>", "location": "<your-azure-location>"}]` |
    | `createEventSubscription` | Specifies whether ... should be created. | `false` |
    | `subnetId` | Specifies the Resource ID of the subnet to which all private endpoints will connect. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.Network/virtualNetworks/<your-vnet-name>/subnets/<your-subnet-name>` |
    | `functionSubnetId` | Specifies the Resource ID of the subnet to which the function will be injected. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.Network/virtualNetworks/<your-vnet-name>/subnets/<your-subnet-name>` |
    | `privateDnsZoneIdBlob` | Specifies the Resource ID of the private DNS zone for Blob Storage. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.Network/privateDnsZones/privatelink.blob.core.windows.net` |
    | `privateDnsZoneIdFile` | Specifies the Resource ID of the private DNS zone for File Storage. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.Network/privateDnsZones/privatelink.file.core.windows.net` |
    | `privateDnsZoneIdKeyVault` | Specifies the Resource ID of the private DNS zone for KeyVault. | `/subscriptions/<your-subscription-id>/resourceGroups/<your-rg-name>/providers/Microsoft.Network/privateDnsZones/privatelink.vaultcore.azure.net` |

6. Commit the changes.
7. Merge the changes into the `main` branch of your repository.
8. Follow the workflow deployment.

## 5. Post-deployment steps

TODO
