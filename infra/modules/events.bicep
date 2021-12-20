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

// Outputs
