// Licensed under the MIT license.

// This template is used to create a System Event Grid Topic.
targetScope = 'resourceGroup'

// Parameters
param tags object
param eventGridTopicName string
param eventGridTopicSourceSubscriptionId string
param eventGridTopicDeadLetterStorageAccountId string
param eventGridTopicDeadLetterStorageAccountContainerName string
param functionId string

// Variables

// Resources
resource eventGridTopic 'Microsoft.EventGrid/systemTopics@2021-12-01' = {
  name: eventGridTopicName
  location: 'global'
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    source: '/subscriptions/${eventGridTopicSourceSubscriptionId}'
    topicType: 'Microsoft.Resources.Subscriptions'
  }
}

resource eventGridEventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2021-12-01' = {
  parent: eventGridTopic
  name: 'service-creation'
  properties: {
    deadLetterWithResourceIdentity: {
      deadLetterDestination: {
        endpointType: 'StorageBlob'
        properties: {
          resourceId: eventGridTopicDeadLetterStorageAccountId
          blobContainerName: eventGridTopicDeadLetterStorageAccountContainerName
        }
      }
    }
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        resourceId: '${functionId}/functions/PurviewAutomation'
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
    }
    eventDeliverySchema: 'EventGridSchema'
    expirationTimeUtc: ''
    filter: {
      includedEventTypes: [
        'Microsoft.Resources.ResourceWriteSuccess'
        'Microsoft.Resources.ResourceDeleteSuccess'
      ]
      enableAdvancedFilteringOnArrays: true
    }
    labels: []
    retryPolicy: {
      eventTimeToLiveInMinutes: 60
      maxDeliveryAttempts: 5
    }
  }
}

// Outputs
