// Licensed under the MIT license.

// This template is used to create Application Insights.
targetScope = 'resourceGroup'

// Parameters
param location string
param tags object
param applicationInsightsName string

// Variables

// Resources
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    DisableIpMasking: false
    DisableLocalAuth: false
    #disable-next-line BCP036
    Flow_Type: 'Redfield'
    ForceCustomerStorageForProfiler: false
    ImmediatePurgeDataOn30Days: true
    IngestionMode: 'ApplicationInsights'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    #disable-next-line BCP036
    Request_Source: 'IbizaWebAppExtensionCreate'
    // SamplingPercentage: 50  // Uncomment, if you want to define the sampling percentage that should be used for the telemetry.
    // WorkspaceResourceId: logAnalyticsWorkspaceId  // Uncomment, if you want to connect your application insights to the central log analytics workspace.
  }
}

// Outputs
output applicationInsightsId string = applicationInsights.id
