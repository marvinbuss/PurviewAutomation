// Copyright (c) Microsoft Corporation.
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
@description('Specifies the resource ID of the central purview instance.')
param purviewId string
@description('Specifies the resource ID of the managed storage of the central purview instance.')
param purviewManagedStorageId string
@description('Specifies the resource ID of the managed event hub of the central purview instance.')
param purviewManagedEventHubId string

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
      'default'
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
    logAnalyticsWorkspaceId: ''
  }
}

