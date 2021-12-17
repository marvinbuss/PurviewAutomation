// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

targetScope = 'resourceGroup'

// Parameters
@description('Specifies the location for all resources.')
param location string
@allowed([
  'dev'
  'tst'
  'prd'
])
@description('Specifies the environment of the deployment.')
param environment string = 'dev'
@minLength(2)
@maxLength(10)
@description('Specifies the prefix for all resources created in this deployment.')
param prefix string
@description('Specifies the tags that you want to apply to all resources.')
param tags object = {}

// Resource Parameters
@description('Specifies the resource ID of the central purview instance.')
param purviewId string
@description('Specifies the resource ID of the managed storage of the central purview instance.')
param purviewManagedStorageId string
@description('Specifies the resource ID of the managed event hub of the central purview instance.')
param purviewManagedEventHubId string
@description('Specifies the name of the purview root collection.')
param purviewRootCollectionName string
@description('Specifies the id of the purview root collection metadata policy.')
param purviewRootCollectionMetadataPolicyId string
@description('Specifies the subscription ids for which event grid topics should be created.')
param eventGridTopicSourceSubscriptionIds array
@description('Specifies whether the event subscription from the event grid topic to the function should be created.')
param createEventSubscription bool

// Network parameters
@description('Specifies the resource ID of the subnet to which all services will connect.')
param subnetId string
@description('Specifies the resource ID of the subnet which will be used for the app service plan.')
param functionSubnetId string

// Private DNS Zone parameters
@description('Specifies the resource ID of the private DNS zone for Blob Storage.')
param privateDnsZoneIdBlob string = ''
@description('Specifies the resource ID of the private DNS zone for File Storage.')
param privateDnsZoneIdFile string = ''
@description('Specifies the resource ID of the private DNS zone for KeyVault.')
param privateDnsZoneIdKeyVault string = ''

// Variables
var name = toLower('${prefix}-${environment}')
var tagsDefault = {
  Owner: 'Data Management and Analytics Scenario'
  Project: 'Data Management and Analytics Scenario'
  Environment: environment
  Toolkit: 'bicep'
  Name: name
}
var tagsJoined = union(tagsDefault, tags)
var storage001Name = '${name}-storage001'
var applicationInsights001Name = '${name}-insights001'
var keyvault001Name = '${name}-vault001'
var function001Name = '${name}-function001'
var function001FileShareName = function001Name
var eventGridTopicDeadLetterStorageAccountContainerName = 'deadletters'

// Resources
module storage001 'modules/services/storage.bicep' = {
  name: 'storage001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tagsJoined
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

module applicationInsights001 'modules/services/applicationinsights.bicep' = {
  name: 'applicationInsights001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tagsJoined
    applicationInsightsName: applicationInsights001Name
  }
}

module keyVault001 'modules/services/keyvault.bicep' = {
  name: 'keyVault001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tagsJoined
    subnetId: subnetId
    keyvaultName: keyvault001Name
    privateDnsZoneIdKeyVault: privateDnsZoneIdKeyVault
    applicationInsightsId: applicationInsights001.outputs.applicationInsightsId
    storageId: storage001.outputs.storageId
  }
}

module function001 'modules/services/function.bicep' = {
  name: 'function001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tags
    functionName: function001Name
    functionSubnetId: functionSubnetId
  }
}

module function001AppSettings 'modules/services/functionAppSettings.bicep' = {
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
    storageConnectionStringSecretUri: keyVault001.outputs.storageConnectionStringSecretUri
    applicationInsightsInstrumentationKeySecretUri: keyVault001.outputs.applicationInsightsInstrumentationKeySecretUri
    applicationInsightsConnectionStringSecretUri: keyVault001.outputs.applicationInsightsConnectionStringSecretUri
  }
}

module roleAssignmentFunctionKeyVault 'modules/auxiliary/keyVaultRoleAssignment.bicep' = {
  name: 'roleAssignmentFunctionKeyVault'
  scope: resourceGroup()
  params: {
    keyVaultId: keyVault001.outputs.keyvaultId
    functionId: function001.outputs.functionId
  }
}

module eventGridTopic 'modules/services/eventgridtopic.bicep' = [for (eventGridTopicSourceSubscriptionId, index) in eventGridTopicSourceSubscriptionIds: {
  name: 'eventGridTopic${padLeft(index, 3, '0')}'
  scope: resourceGroup()
  params: {
    tags: tags
    eventGridTopicName: '${name}-eventGrid${padLeft(index + 1, 3, '0')}'
    eventGridTopicSourceSubscriptionId: eventGridTopicSourceSubscriptionId
    eventGridTopicDeadLetterStorageAccountId: storage001.outputs.storageId
    eventGridTopicDeadLetterStorageAccountContainerName: eventGridTopicDeadLetterStorageAccountContainerName
    functionId: function001.outputs.functionId
    createEventSubscription: createEventSubscription
  }
}]

module functionSubscriptionRoleAssignmentContributor 'modules/auxiliary/functionRoleAssignmentSubscription.bicep' = {
  name: 'functionSubscriptionRoleAssignmentContributor'
  scope: subscription()
  params: {
    role: 'Contributor'
    functionId: function001.outputs.functionId
  }
}

module functionSubscriptionRoleAssignmentUserAccessAdministrator 'modules/auxiliary/functionRoleAssignmentSubscription.bicep' = {
  name: 'functionSubscriptionRoleAssignmentUserAccessAdministrator'
  scope: subscription()
  params: {
    role: 'UserAccessAdministrator'
    functionId: function001.outputs.functionId
  }
}

// Outputs
output function001Name string = function001.outputs.functionName
output function001PrincipalId string = function001.outputs.functionPrincipalId
