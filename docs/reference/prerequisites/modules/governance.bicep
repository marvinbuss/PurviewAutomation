// Licensed under the MIT license.

// This template is used as a module from the main.bicep template. 
// The module contains a template to create the governance services.
targetScope = 'resourceGroup'

// Parameters
param location string
param prefix string
param tags object
param subnetId string
param privateDnsZoneIdPurview string = ''
param privateDnsZoneIdPurviewPortal string = ''
param privateDnsZoneIdStorageBlob string = ''
param privateDnsZoneIdStorageQueue string = ''
param privateDnsZoneIdEventhubNamespace string = ''

// Variables
var purview001Name = '${prefix}-purview001'

// Resources
module purview001 'services/purview.bicep' = {
  name: 'purview001'
  scope: resourceGroup()
  params: {
    location: location
    tags: tags
    subnetId: subnetId
    purviewName: purview001Name
    privateDnsZoneIdPurview: privateDnsZoneIdPurview
    privateDnsZoneIdPurviewPortal: privateDnsZoneIdPurviewPortal
    privateDnsZoneIdStorageBlob: privateDnsZoneIdStorageBlob
    privateDnsZoneIdStorageQueue: privateDnsZoneIdStorageQueue
    privateDnsZoneIdEventhubNamespace: privateDnsZoneIdEventhubNamespace
  }
}

// Outputs
output purviewId string = purview001.outputs.purviewId
output purviewManagedStorageId string = purview001.outputs.purviewManagedStorageId
output purviewManagedEventHubId string = purview001.outputs.purviewManagedEventHubId
