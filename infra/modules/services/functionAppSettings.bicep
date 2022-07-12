// Licensed under the MIT license.

// This template is used to create Application Insights.
targetScope = 'resourceGroup'

// Parameters
param functionId string
param functionFileShareName string
param applicationInsightsInstrumentationKeySecretUri string
param applicationInsightsConnectionStringSecretUri string
param storageConnectionStringSecretUri string
param purviewId string
param purviewManagedStorageId string
param purviewManagedEventHubId string
#disable-next-line no-unused-params
param purviewRootCollectionName string
param purviewRootCollectionMetadataPolicyId string
param repositoryUrl string = ''

// Variables
var functionName = length(split(functionId, '/')) == 9 ? last(split(functionId, '/')) : 'incorrectSegmentLength'
var purviewName = length(split(purviewId, '/')) == 9 ? last(split(purviewId, '/')) : 'incorrectSegmentLength'

// Resources
resource function 'Microsoft.Web/sites@2022-03-01' existing = {
  name: functionName
}

resource functionAppSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: function
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${storageConnectionStringSecretUri})'
    WEBSITE_CONTENTSHARE: functionFileShareName
    WEBSITE_RUN_FROM_PACKAGE: empty(repositoryUrl) ? '1' : '0'
    APPINSIGHTS_INSTRUMENTATIONKEY: '@Microsoft.KeyVault(SecretUri=${applicationInsightsInstrumentationKeySecretUri})'
    APPLICATIONINSIGHTS_CONNECTION_STRING: '@Microsoft.KeyVault(SecretUri=${applicationInsightsConnectionStringSecretUri})'
    APPINSIGHTS_PROFILERFEATURE_VERSION: '1.0.0'
    ApplicationInsightsAgent_EXTENSION_VERSION: '~2'
    DiagnosticServices_EXTENSION_VERSION: '~3'
    AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${storageConnectionStringSecretUri})'
    WEBSITE_CONTENTOVERVNET: '1'
    WEBSITE_VNET_ROUTE_ALL: '1'
    PROJECT: empty(repositoryUrl) ? '' : 'code/PurviewAutomation/PurviewAutomation.csproj'
    SCM_COMMAND_IDLE_TIMEOUT: '120'
    FunctionPrincipalId: function.identity.principalId
    PurviewResourceId: purviewId
    PurviewManagedStorageId: purviewManagedStorageId
    PurviewManagedEventHubId: purviewManagedEventHubId
    PurviewRootCollectionName: purviewName
    PurviewRootCollectionMetadataPolicyId: purviewRootCollectionMetadataPolicyId
    ManagedIntegrationRuntimeName: 'defaultIntegrationRuntime'
    UseManagedPrivateEndpoints: 'True'
    SetupScan: 'True'
    TriggerScan: 'True'
    SetupLineage: 'True'
    RemoveDataSources: 'True'
  }
}

resource functionSourceControl 'Microsoft.Web/sites/sourcecontrols@2022-03-01' = if(!empty(repositoryUrl)) {
  parent: function
  name: 'web'
  dependsOn: [
    functionAppSettings
  ]
  properties: {
    repoUrl: repositoryUrl
    branch: 'main'
    isManualIntegration: true
  }
}

// Outputs
