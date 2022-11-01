// Licensed under the MIT license.

targetScope = 'subscription'

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
@description('Specifies the repository URL in which the function app is checked in.')
param repositoryUrl string
@description('Specifies the resource ID of the central purview instance.')
param purviewId string
@description('Specifies the resource ID of the managed storage of the central purview instance.')
param purviewManagedStorageId string
@description('Specifies the resource ID of the managed event hub of the central purview instance.')
param purviewManagedEventHubId string
@description('Specifies the id of the purview root collection metadata policy.')
param purviewRootCollectionMetadataPolicyId string
@description('Specifies the subscription ids for which event grid topics should be created.')
param eventGridTopicSourceSubscriptions array
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
  Owner: 'Purview Automation'
  Project: 'Purview Automation'
  Environment: environment
  Toolkit: 'bicep'
  Name: name
}
var tagsJoined = union(tagsDefault, tags)
var purviewRootCollectionName = last(split(purviewId, '/'))
var eventGridTopicDeadLetterStorageAccountContainerName = 'deadletters'
var functionResourceGroupName = '${name}-function'
var eventsResourceGroupName = '${name}-events'
var automationResourceGroupName = '${name}-automation'

// Function Resources
resource functionResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: functionResourceGroupName
  location: location
  tags: tagsJoined
  properties: {}
}

module functionResources 'modules/function.bicep' = {
  name: 'functionResources'
  scope: functionResourceGroup
  params: {
    location: location
    prefix: name
    tags: tagsJoined
    eventGridTopicDeadLetterStorageAccountContainerName: eventGridTopicDeadLetterStorageAccountContainerName
    subnetId: subnetId
    functionSubnetId: functionSubnetId
    repositoryUrl: repositoryUrl
    purviewId: purviewId
    purviewManagedStorageId: purviewManagedStorageId
    purviewManagedEventHubId: purviewManagedEventHubId
    purviewRootCollectionName: purviewRootCollectionName
    purviewRootCollectionMetadataPolicyId: purviewRootCollectionMetadataPolicyId
    privateDnsZoneIdKeyVault: privateDnsZoneIdKeyVault
    privateDnsZoneIdFile: privateDnsZoneIdFile
    privateDnsZoneIdBlob: privateDnsZoneIdBlob
  }
}

// Event Resources
module eventsResourceGroups 'modules/auxiliary/createResourceGroup.bicep' = [for (eventGridTopicSourceSubscription, index) in eventGridTopicSourceSubscriptions: {
  name: 'eventsResourceGroup${padLeft(index + 1, 3, '0')}'
  scope: subscription(eventGridTopicSourceSubscription.subscriptionId)
  params: {
    location: eventGridTopicSourceSubscription.location
    tags: tagsJoined
    resourceGroupName: eventsResourceGroupName
  }
}]

module eventsResources 'modules/events.bicep' = [for (eventGridTopicSourceSubscription, index) in eventGridTopicSourceSubscriptions: {
  name: 'eventsResources${padLeft(index + 1, 3, '0')}'
  scope: resourceGroup(eventGridTopicSourceSubscription.subscriptionId, eventsResourceGroupName)
  dependsOn: [
    eventsResourceGroups
  ]
  params: {
    prefix: name
    tags: tagsJoined
    eventGridTopicSourceSubscriptionId: eventGridTopicSourceSubscription.subscriptionId
    createEventSubscription: createEventSubscription
    eventGridTopicDeadLetterStorageAccountContainerName: eventGridTopicDeadLetterStorageAccountContainerName
    functionId: functionResources.outputs.function001Id
    storageId: functionResources.outputs.storage001Id
    purviewId: purviewId
  }
}]

// Automation Resources
resource automationResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: automationResourceGroupName
  location: location
  tags: tagsJoined
  properties: {}
}

module automationResources 'modules/automation.bicep' = {
  name: 'automationResources'
  scope: automationResourceGroup
  params: {
    location: location
    tags: tagsJoined
    prefix: name
    purviewId: purviewId
    purviewRootCollectionAdminObjectIds: [
      functionResources.outputs.function001PrincipalId
    ]
  }
}

// Outputs
output function001Name string = functionResources.outputs.function001Name
output function001PrincipalId string = functionResources.outputs.function001PrincipalId
