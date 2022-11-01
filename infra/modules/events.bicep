// Licensed under the MIT license.

// This template is used as a module from the main.bicep template.
// The module contains a template to create the event services.
targetScope = 'resourceGroup'

// Parameters
param prefix string
param tags object
param eventGridTopicSourceSubscriptionId string
param eventGridTopicDeadLetterStorageAccountContainerName string
param createEventSubscription bool
param storageId string
param functionId string
param purviewId string

// Variables
var eventGridTopicName = '${prefix}-eventGrid001'

// Resources
module eventGridTopic 'services/eventgridtopic.bicep' = {
  name: 'eventGridTopic'
  scope: resourceGroup()
  params: {
    tags: tags
    eventGridTopicName: eventGridTopicName
    eventGridTopicSourceSubscriptionId: eventGridTopicSourceSubscriptionId
    eventGridTopicDeadLetterStorageAccountId: storageId
    eventGridTopicDeadLetterStorageAccountContainerName: eventGridTopicDeadLetterStorageAccountContainerName
    functionId: functionId
    createEventSubscription: createEventSubscription
  }
}

// Role assignments
module functionSubscriptionRoleAssignmentReader 'auxiliary/functionRoleAssignmentSubscription.bicep' = {
  name: 'functionSubscriptionRoleAssignmentReader'
  scope: subscription()
  params: {
    role: 'Reader'
    functionId: functionId
  }
}

module functionSubscriptionRoleAssignmentContributor 'auxiliary/functionRoleAssignmentSubscription.bicep' = {
  name: 'functionSubscriptionRoleAssignmentContributor'
  scope: subscription()
  params: {
    role: 'Contributor'
    functionId: functionId
  }
}

module functionSubscriptionRoleAssignmentUserAccessAdministrator 'auxiliary/functionRoleAssignmentSubscription.bicep' = {
  name: 'functionSubscriptionRoleAssignmentUserAccessAdministrator'
  scope: subscription()
  params: {
    role: 'UserAccessAdministrator'
    functionId: functionId
  }
}

module purviewSubscriptionRoleAssignmentReader 'auxiliary/purviewRoleAssignmentSubscription.bicep' = {
  name: 'purviewSubscriptionRoleAssignmentReader'
  scope: subscription()
  params: {
    purviewId: purviewId
    role: 'Reader'
  }
}

module purviewSubscriptionRoleAssignmentStorageBlobReader 'auxiliary/purviewRoleAssignmentSubscription.bicep' = {
  name: 'purviewSubscriptionRoleAssignmentStorageBlobReader'
  scope: subscription()
  params: {
    purviewId: purviewId
    role: 'StorageBlobDataReader'
  }
}

// Outputs
