// Licensed under the MIT license.

// This template is used to create Private DNS Zones.
targetScope = 'resourceGroup'

// Parameters
param vnetId string
param tags object

// Variables
var vnetName = length(split(vnetId, '/')) >= 9 ? last(split(vnetId, '/')) : 'incorrectSegmentLength'
var privateDnsZoneNames = [
  'privatelink.adf.azure.com'
  'privatelink.azuresynapse.net'
  'privatelink.azurewebsites.net'
  'privatelink.blob.${environment().suffixes.storage}'
  'privatelink.cassandra.cosmos.azure.com'
  'privatelink${environment().suffixes.sqlServerHostname}'
  'privatelink.datafactory.azure.net'
  'privatelink.dev.azuresynapse.net'
  'privatelink.dfs.${environment().suffixes.storage}'
  'privatelink.documents.azure.com'
  'privatelink.file.${environment().suffixes.storage}'
  'privatelink.gremlin.cosmos.azure.com'
  'privatelink.mariadb.database.azure.com'
  'privatelink.mongo.cosmos.azure.com'
  'privatelink.mysql.database.azure.com'
  'privatelink.postgres.database.azure.com'
  'privatelink.purview.azure.com'
  'privatelink.purviewstudio.azure.com'
  'privatelink.queue.${environment().suffixes.storage}'
  'privatelink.servicebus.windows.net'
  'privatelink.sql.azuresynapse.net'
  'privatelink.table.${environment().suffixes.storage}'
  'privatelink.table.cosmos.azure.com'
  'privatelink.vaultcore.azure.net'
  'privatelink.web.${environment().suffixes.storage}'
]

// Resources
resource privateDnsZones 'Microsoft.Network/privateDnsZones@2020-06-01' = [for item in privateDnsZoneNames: {
  name: item
  location: 'global'
  tags: tags
  properties: {}
}]

resource virtualNetworkLinks 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = [for item in privateDnsZoneNames: {
  name: '${item}/${vnetName}'
  location: 'global'
  dependsOn: [
    privateDnsZones
  ]
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}]

// Outputs
output privateDnsZoneIdDataFactory string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.datafactory.azure.net'
output privateDnsZoneIdDataFactoryPortal string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.adf.azure.com'
output privateDnsZoneIdAppService string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.azurewebsites.net'
output privateDnsZoneIdCosmosdbCassandra string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.cassandra.cosmos.azure.com'
output privateDnsZoneIdCosmosdbSql string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.documents.azure.com'
output privateDnsZoneIdCosmosdbGremlin string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.gremlin.cosmos.azure.com'
output privateDnsZoneIdCosmosdbMongo string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.mongo.cosmos.azure.com'
output privateDnsZoneIdCosmosdbTable string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.table.cosmos.azure.com'
output privateDnsZoneIdSqlServer string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink${environment().suffixes.sqlServerHostname}'
output privateDnsZoneIdMySqlServer string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.mysql.database.azure.com'
output privateDnsZoneIdMariaDb string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.mariadb.database.azure.com'
output privateDnsZoneIdPostgreSql string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.postgres.database.azure.com'
output privateDnsZoneIdPurview string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.purview.azure.com'
output privateDnsZoneIdPurviewPortal string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.purviewstudio.azure.com'
output privateDnsZoneIdDfs string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.dfs.${environment().suffixes.storage}'
output privateDnsZoneIdBlob string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.blob.${environment().suffixes.storage}'
output privateDnsZoneIdFile string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.file.${environment().suffixes.storage}'
output privateDnsZoneIdQueue string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.queue.${environment().suffixes.storage}'
output privateDnsZoneIdTable string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.table.${environment().suffixes.storage}'
output privateDnsZoneIdWeb string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.web.${environment().suffixes.storage}'
output privateDnsZoneIdNamespace string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.servicebus.windows.net'
output privateDnsZoneIdKeyVault string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.vaultcore.azure.net'
output privateDnsZoneIdSynapse string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.azuresynapse.net'
output privateDnsZoneIdSynapseDev string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.dev.azuresynapse.net'
output privateDnsZoneIdSynapseSql string = '${resourceGroup().id}/providers/Microsoft.Network/privateDnsZones/privatelink.sql.azuresynapse.net'
