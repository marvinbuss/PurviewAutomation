// Licensed under the MIT license.

// The module contains a template to deploy secrets to a Key Vault.
targetScope = 'resourceGroup'

// Parameters
param keyVaultId string
param applicationInsightsId string
param storageId string

// Variables
var keyVaultName = length(split(keyVaultId, '/')) == 9 ? last(split(keyVaultId, '/')) : 'incorrectSegmentLength'
var applicationInsightsSubscriptionId = length(split(applicationInsightsId, '/')) >= 9 ? split(applicationInsightsId, '/')[2] : subscription().subscriptionId
var applicationInsightsResourceGroupName = length(split(applicationInsightsId, '/')) >= 9 ? split(applicationInsightsId, '/')[4] : resourceGroup().name
var applicationInsightsName = length(split(applicationInsightsId, '/')) >= 9 ? last(split(applicationInsightsId, '/')) : 'incorrectSegmentLength'
var storageSubscriptionId = length(split(storageId, '/')) >= 9 ? split(storageId, '/')[2] : subscription().subscriptionId
var storageResourceGroupName = length(split(storageId, '/')) >= 9 ? split(storageId, '/')[4] : resourceGroup().name
var storageName = length(split(storageId, '/')) >= 9 ? last(split(storageId, '/')) : 'incorrectSegmentLength'
var applicationInsightsConnectionStringSecretName = 'applicationInsightsConnectionString'
var applicationInsightsInstrumentationKeySecretName = 'applicationInsightsInstrumentationKey'
var storageConnectionStringSecretName = 'storageConnectionString'

// Resources
resource keyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' existing = {
  name: keyVaultName
}

resource storage 'Microsoft.Storage/storageAccounts@2021-06-01' existing = {
  name: storageName
  scope: resourceGroup(storageSubscriptionId, storageResourceGroupName)
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
  scope: resourceGroup(applicationInsightsSubscriptionId, applicationInsightsResourceGroupName)
}

resource applicationInsightsConnectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: keyVault
  name: applicationInsightsConnectionStringSecretName
  properties: {
    contentType: 'text/plain'
    value: reference(applicationInsightsId, '2020-02-02').ConnectionString
    attributes: {
      enabled: true
    }
  }
}

resource applicationInsightsInstrumentationKey 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: keyVault
  name: applicationInsightsInstrumentationKeySecretName
  properties: {
    contentType: 'text/plain'
    value: reference(applicationInsightsId, '2020-02-02').InstrumentationKey
    attributes: {
      enabled: true
    }
  }
}

resource storageConnectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: keyVault
  name: storageConnectionStringSecretName
  properties: {
    contentType: 'text/plain'
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageName};AccountKey=${listKeys(storageId, '2021-06-01').keys[0].value};EndpointSuffix=core.windows.net'
    attributes: {
      enabled: true
    }
  }
}

// Outputs
output applicationInsightsConnectionStringSecretUri string = applicationInsightsConnectionString.properties.secretUri
output applicationInsightsInstrumentationKeySecretUri string = applicationInsightsInstrumentationKey.properties.secretUri
output storageConnectionStringSecretUri string = storageConnectionString.properties.secretUri
