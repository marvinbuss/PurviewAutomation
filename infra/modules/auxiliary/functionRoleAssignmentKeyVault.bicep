// Licensed under the MIT license.

// This template is used to create a role assignment to Key Vault.
targetScope = 'resourceGroup'

// Parameters
param keyVaultId string
param functionId string

// Variables
var keyVaultName = length(split(keyVaultId, '/')) == 9 ? last(split(keyVaultId, '/')) : 'incorrectSegmentLength'
var functionName = length(split(functionId, '/')) == 9 ? last(split(functionId, '/')) : 'incorrectSegmentLength'

// Resources
resource keyVault 'Microsoft.KeyVault/vaults@2021-06-01-preview' existing = {
  name: keyVaultName
}

resource function 'Microsoft.Web/sites@2021-02-01' existing = {
  name: functionName
}

resource functionRoleAssignmentKeyVault 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(uniqueString(keyVault.id, function.id))
  scope: keyVault
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: function.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
