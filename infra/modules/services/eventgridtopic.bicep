// Licensed under the MIT license.

// This template is used to create a System Event Grid Topic.
targetScope = 'resourceGroup'

// Parameters
param tags object
param eventGridTopicName string
param eventGridTopicSourceSubscriptionId string
#disable-next-line no-unused-params
param eventGridTopicDeadLetterStorageAccountId string
#disable-next-line no-unused-params
param eventGridTopicDeadLetterStorageAccountContainerName string
param functionId string
param createEventSubscription bool

// Variables

// Resources
resource eventGridTopic 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
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

resource eventGridEventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2022-06-15' = if(createEventSubscription) {
  parent: eventGridTopic
  name: 'service-creation'
  properties: {
    // deadLetterWithResourceIdentity: {
    //   identity: {
    //     type: 'SystemAssigned'
    //   }
    //   deadLetterDestination: {
    //     endpointType: 'StorageBlob'
    //     properties: {
    //       resourceId: eventGridTopicDeadLetterStorageAccountId
    //       blobContainerName: eventGridTopicDeadLetterStorageAccountContainerName
    //     }
    //   }
    // }
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        #disable-next-line use-resource-id-functions
        resourceId: '${functionId}/functions/PurviewAutomation'
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
    }
    eventDeliverySchema: 'EventGridSchema'
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
output eventGridTopicId string = eventGridTopic.id
