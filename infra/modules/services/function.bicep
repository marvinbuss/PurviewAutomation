// Licensed under the MIT license.

// This template is used to create Application Insights.
targetScope = 'resourceGroup'

// Parameters
param location string
param tags object
param functionName string
param functionSubnetId string

// Variables
var appServicePlanName = '${functionName}-asp001'

// Resources
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
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

resource function 'Microsoft.Web/sites@2022-03-01' = {
  name: functionName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'functionapp'
  properties: {
    clientAffinityEnabled: false
    clientCertEnabled: false
    clientCertMode: 'Required'
    enabled: true
    hostNamesDisabled: false
    httpsOnly: true
    hyperV: false
    isXenon: false
    keyVaultReferenceIdentity: 'SystemAssigned'
    reserved: false
    redundancyMode: 'None'
    scmSiteAlsoStopped: false
    serverFarmId: appServicePlan.id
    storageAccountRequired: false
    virtualNetworkSubnetId: functionSubnetId
    siteConfig: {
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
      }
      acrUseManagedIdentityCreds: false
      alwaysOn: false
      functionAppScaleLimit: 200
      http20Enabled: false
      // linuxFxVersion: 'DOTNETCORE|6.0'
      minTlsVersion: '1.2'
      minimumElasticInstanceCount: 1
      netFrameworkVersion: 'v6.0'
      numberOfWorkers: 1
      use32BitWorkerProcess: true
      vnetRouteAllEnabled: true
    }
  }
}

// Outputs
output functionName string = function.name
output functionPrincipalId string = function.identity.principalId
output functionId string = function.id
