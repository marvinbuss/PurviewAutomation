// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// This template is used to create a KeyVault.
targetScope = 'resourceGroup'

// Parameters
param location string
param tags object
param subnetId string
param keyvaultName string
param privateDnsZoneIdKeyVault string = ''
param applicationInsightsId string
param storageId string

// Variables
var keyVaultPrivateEndpointName = '${keyVault.name}-private-endpoint'
var storageAccountName = length(split(storageId, '/')) == 9 ? last(split(storageId, '/')) : 'incorrectSegmentLength'

// Resources
resource keyVault 'Microsoft.KeyVault/vaults@2021-04-01-preview' = {
  name: keyvaultName
  location: location
  tags: tags
  properties: {
    accessPolicies: []
    createMode: 'default'
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enablePurgeProtection: true
    enableRbacAuthorization: true
    enableSoftDelete: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: []
    }
    sku: {
      family: 'A'
      name: 'standard'
    }
    softDeleteRetentionInDays: 7
    tenantId: subscription().tenantId
  }
}

resource applicationInsightsInstrumentationKey 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: keyVault
  name: 'applicationInsightsInstrumentationKey'
  properties: {
    contentType: 'text/plain'
    value: reference(applicationInsightsId, '2020-02-02').InstrumentationKey
    attributes: {
      enabled: true
    }
  }
}

resource applicationInsightsConnectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: keyVault
  name: 'applicationInsightsConnectionString'
  properties: {
    contentType: 'text/plain'
    value: reference(applicationInsightsId, '2020-02-02').ConnectionString
    attributes: {
      enabled: true
    }
  }
}

resource storageConnectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: keyVault
  name: 'storageConnectionString'
  properties: {
    contentType: 'text/plain'
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageId, '2021-06-01').keys[0].value};EndpointSuffix=core.windows.net'
    attributes: {
      enabled: true
    }
  }
}

resource keyVaultPrivateEndpoint 'Microsoft.Network/privateEndpoints@2020-11-01' = {
  name: keyVaultPrivateEndpointName
  location: location
  tags: tags
  properties: {
    manualPrivateLinkServiceConnections: []
    privateLinkServiceConnections: [
      {
        name: keyVaultPrivateEndpointName
        properties: {
          groupIds: [
            'vault'
          ]
          privateLinkServiceId: keyVault.id
          requestMessage: ''
        }
      }
    ]
    subnet: {
      id: subnetId
    }
  }
}

resource keyVaultPrivateEndpointARecord 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2020-11-01' = if (!empty(privateDnsZoneIdKeyVault)) {
  parent: keyVaultPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: '${keyVaultPrivateEndpoint.name}-arecord'
        properties: {
          privateDnsZoneId: privateDnsZoneIdKeyVault
        }
      }
    ]
  }
}

// Outputs
output keyvaultId string = keyVault.id
output applicationInsightsConnectionStringSecretUri string = applicationInsightsConnectionString.properties.secretUri
output applicationInsightsInstrumentationKeySecretUri string = applicationInsightsInstrumentationKey.properties.secretUri
output storageConnectionStringSecretUri string = storageConnectionString.properties.secretUri
