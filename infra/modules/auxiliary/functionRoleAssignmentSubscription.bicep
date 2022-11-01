// Licensed under the MIT license.

// The module contains a template to create a role assignment to a Subscription.
targetScope = 'subscription'

// Parameters
param functionId string
@allowed([
  'Reader'
  'Contributor'
  'UserAccessAdministrator'
])
param role string

// Variables
var functionSubscriptionId = length(split(functionId, '/')) == 9 ? split(functionId, '/')[2] : subscription().subscriptionId
var functionGroupName = length(split(functionId, '/')) == 9 ? split(functionId, '/')[4] : 'incorrectSegmentLength'
var functionName = length(split(functionId, '/')) == 9 ? last(split(functionId, '/')) : 'incorrectSegmentLength'
var roles = {
  Reader: 'acdd72a7-3385-48ef-bd42-f606fba81ae7'
  Contributor: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
  UserAccessAdministrator: '18d7d88d-d35e-4fb5-a5c3-7773c20a72d9'
}

// Resources
resource function 'Microsoft.Web/sites@2021-02-01' existing = {
  name: functionName
  scope: resourceGroup(functionSubscriptionId, functionGroupName)
}

resource functionRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(uniqueString(subscription().subscriptionId, function.id, roles[role]))
  scope: subscription()
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roles[role])
    principalId: function.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
