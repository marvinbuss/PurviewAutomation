// Licensed under the MIT license.

// This template is used as a module from the main.bicep template.
// The module contains a template to create a resource group.
targetScope = 'subscription'

// Parameters
param location string
param prefix string
param tags object

// Variables

// Resources
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${prefix}-events'
  location: location
  tags: tags
  properties: {}
}

// Outputs
output subscriptionId string = subscription().subscriptionId
output resourceGroupName string = resourceGroup.name
