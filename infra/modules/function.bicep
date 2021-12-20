// Licensed under the MIT license.

// This template is used as a module from the main.bicep template.
// The module contains a template to create the event services.
targetScope = 'resourceGroup'

// Parameters
param location string
param prefix string
param tags object
param eventGridTopicDeadLetterStorageAccountContainerName string
param subnetId string
param functionSubnetId string
param purviewId string
param purviewManagedEventHubId string
param purviewManagedStorageId string
param purviewRootCollectionName string
param purviewRootCollectionMetadataPolicyId string
param privateDnsZoneIdBlob string
param privateDnsZoneIdFile string
param privateDnsZoneIdKeyVault string

// Variables
var storage001Name = '${prefix}-storage001'
var applicationInsights001Name = '${prefix}-insights001'
var keyvault001Name = '${prefix}-vault001'
var function001Name = '${prefix}-function001'
var function001FileShareName = function001Name

// Resources
module storage001 'services/storage.bicep' = {
  name: 'storage001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tags
    subnetId: subnetId
    storageName: storage001Name
    storageContainerNames: [
      eventGridTopicDeadLetterStorageAccountContainerName
    ]
    storageFileShareNames: [
      function001FileShareName
    ]
    storageSkuName: 'Standard_LRS'
    privateDnsZoneIdBlob: privateDnsZoneIdBlob
    privateDnsZoneIdFile: privateDnsZoneIdFile
  }
}

module applicationInsights001 'services/applicationinsights.bicep' = {
  name: 'applicationInsights001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tags
    applicationInsightsName: applicationInsights001Name
  }
}

module keyVault001 'services/keyvault.bicep' = {
  name: 'keyVault001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tags
    subnetId: subnetId
    keyvaultName: keyvault001Name
    privateDnsZoneIdKeyVault: privateDnsZoneIdKeyVault
  }
}

module keyvault001Secrets 'auxiliary/keyVaultSecretDeployment.bicep' = {
  name: 'keyvault001Secrets'
  scope: resourceGroup()
  params: {
    keyVaultId: keyVault001.outputs.keyvaultId
    applicationInsightsId: applicationInsights001.outputs.applicationInsightsId
    storageId: storage001.outputs.storageId
  }
}

module function001 'services/function.bicep' = {
  name: 'function001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tags
    functionName: function001Name
    functionSubnetId: functionSubnetId
  }
}

module function001AppSettings 'services/functionAppSettings.bicep' = {
  name: 'function001AppSettings'
  scope: resourceGroup()
  dependsOn: [
    roleAssignmentFunctionKeyVault
  ]
  params: {
    functionId: function001.outputs.functionId
    purviewId: purviewId
    purviewManagedEventHubId: purviewManagedEventHubId
    purviewManagedStorageId: purviewManagedStorageId
    purviewRootCollectionName: purviewRootCollectionName
    purviewRootCollectionMetadataPolicyId: purviewRootCollectionMetadataPolicyId
    functionFileShareName: function001FileShareName
    storageConnectionStringSecretUri: keyvault001Secrets.outputs.storageConnectionStringSecretUri
    applicationInsightsInstrumentationKeySecretUri: keyvault001Secrets.outputs.applicationInsightsInstrumentationKeySecretUri
    applicationInsightsConnectionStringSecretUri: keyvault001Secrets.outputs.applicationInsightsConnectionStringSecretUri
  }
}

module roleAssignmentFunctionKeyVault 'auxiliary/keyVaultRoleAssignment.bicep' = {
  name: 'roleAssignmentFunctionKeyVault'
  scope: resourceGroup()
  params: {
    keyVaultId: keyVault001.outputs.keyvaultId
    functionId: function001.outputs.functionId
  }
}

module functionSubscriptionRoleAssignmentContributor 'auxiliary/functionRoleAssignmentSubscription.bicep' = {
  name: 'functionSubscriptionRoleAssignmentContributor'
  scope: subscription()
  params: {
    role: 'Contributor'
    functionId: function001.outputs.functionId
  }
}

module functionSubscriptionRoleAssignmentUserAccessAdministrator 'auxiliary/functionRoleAssignmentSubscription.bicep' = {
  name: 'functionSubscriptionRoleAssignmentUserAccessAdministrator'
  scope: subscription()
  params: {
    role: 'UserAccessAdministrator'
    functionId: function001.outputs.functionId
  }
}

// Outputs
output storage001Id string = storage001.outputs.storageId
output function001Id string = function001.outputs.functionId
output function001Name string = function001.outputs.functionName
output function001PrincipalId string = function001.outputs.functionPrincipalId
