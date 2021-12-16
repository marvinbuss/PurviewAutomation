// Licensed under the MIT license.

// This template is used to create Application Insights.
targetScope = 'resourceGroup'

// Parameters
param location string
param tags object
param functionName string
param applicationInsightsInstrumentationKeySecretUri string
param applicationInsightsConnectionStringSecretUri string
param storageConnectionStringSecretUri string
param functionSubnetId string
param purviewId string

// Variables
var appServicePlanName = '${functionName}-asp001'

// Resources
resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  kind: ''
  sku: {
    tier: 'Standard'
    name: 'S1'
  }
  properties: {
    elasticScaleEnabled: false
    hyperV: false
    isSpot: false
    reserved: false
    isXenon: false
    maximumElasticWorkerCount: 1
    perSiteScaling: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
    zoneRedundant: false
  }
}

resource function 'Microsoft.Web/sites@2021-02-01' = {
  name: functionName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'functionapp'
  properties: {
    clientAffinityEnabled: false
    enabled: true
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: '@Microsoft.KeyVault(SecretUri=${applicationInsightsInstrumentationKeySecretUri})'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: '@Microsoft.KeyVault(SecretUri=${applicationInsightsConnectionStringSecretUri})'
        }
        {
          name: 'APPINSIGHTS_PROFILERFEATURE_VERSION'
          value: '1.0.0'
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'DiagnosticServices_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'AzureWebJobsStorage'
          value: '@Microsoft.KeyVault(SecretUri=${storageConnectionStringSecretUri})'
        }
        {
          name: 'FunctionPrincipalId'
          value: 'TODO'  // TODO
        }
      ]
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
      }
      alwaysOn: true
      minTlsVersion: '1.2'
      // http20Enabled: true
      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: true
      vnetRouteAllEnabled: true
    }
    serverFarmId: appServicePlan.id
    virtualNetworkSubnetId: functionSubnetId
  }
}

resource test 'Microsoft.Web/sites/config@2021-02-01' = {
  parent: function
  name: 'appsettings'
  properties: {
    
  }
}

// Outputs
