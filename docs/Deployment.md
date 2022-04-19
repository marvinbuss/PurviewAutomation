# Deployment Guide

Please follow the guide below to deploy the solution into your own tenant and subscription. 

## 1. Fork the repository

First, you must fork this repository. To do so, please follow the steps below:

1. Log in to GitHub using your GitHub account.
2. On GitHub, navigate to the [main page of this repository](https://github.com/marvinbuss/PurviewAutomation).
3. In the top-right corner of the page, click **Fork**.

    ![Fork GitHub repository](/docs/images/ForkRepository.png)

4. Select a repository name and description and click **Create Fork**.

## 2. Create Service Principal

A service principal with *Contributor* and *User Access Administrator* rights needs to be generated for authentication and authorization from GitHubto your Azure subscription. This is required to deploy resources to your environment. Follow the steps below to create a Service Principle:

1. Go to the Azure Portal to find the ID of your subscription.
2. Start the Cloud Shell and login to Azure using `az login` when using Azure CLI or `Connect-AzAccount` when using Azure PowerShell.
3. Set the Azure context using `az account set --subscription "<your-subscription-id>"` when using Azure CLI or `Set-AzContext -Subscription "<your-subscription-id>"` when using Azure PowerShell.
4. Execute the following commands using Azure CLI to generate the required Service Pirncipal credentials:

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

    # Replace <your-service-principal-name> and <your-subscription-id> with your
    # Azure subscription id and any name for your service principal.
    az role assignment create \
        --assignee "<your-service-principle-object-id>" \
        --role "User Access Administrator" \
        --scopes "/subscription/<your-subscription-id>"
    ```

    **Azure PowerShell:**
    
    ```powershell
    # Get Service Principal Object ID
    $spObjectId = (Get-AzADServicePrincipal -DisplayName "<your-service-principal-name>").id

    # Add role assignment
    # For Resource Scope level assignment
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

## 4. 
