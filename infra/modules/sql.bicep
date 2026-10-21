targetScope = 'resourceGroup'

param name string
param location string = resourceGroup().location
param tags object = {}

@description('Login label for the Entra admin (the user-assigned managed identity name).')
param adminLogin string

@description('Object (principal) id of the user-assigned managed identity that acts as the Entra admin.')
param adminSid string

@description('Databases to create on the logical server.')
param databaseNames array

@description('Apply the Azure SQL free offer to each database.')
param useFreeLimit bool = true

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: adminLogin
      sid: adminSid
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow other Azure services (including Container Apps egress) to reach the server.
resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource databases 'Microsoft.Sql/servers/databases@2023-08-01-preview' = [
  for dbName in databaseNames: {
    parent: sqlServer
    name: dbName
    location: location
    tags: tags
    sku: {
      name: 'GP_S_Gen5'
      tier: 'GeneralPurpose'
      family: 'Gen5'
      capacity: 2
    }
    properties: {
      collation: 'SQL_Latin1_General_CP1_CI_AS'
      autoPauseDelay: 60
      minCapacity: json('0.5')
      maxSizeBytes: 34359738368
      useFreeLimit: useFreeLimit
      freeLimitExhaustionBehavior: useFreeLimit ? 'AutoPause' : null
      zoneRedundant: false
    }
  }
]

output fqdn string = sqlServer.properties.fullyQualifiedDomainName
output serverName string = sqlServer.name
