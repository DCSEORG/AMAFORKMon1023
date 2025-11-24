// Azure SQL Database Bicep module
@description('Location for all resources')
param location string

@description('Base name for resources')
param baseName string

@description('Managed Identity Principal ID for role assignments')
param managedIdentityPrincipalId string

@description('Managed Identity Name for database user')
param managedIdentityName string

var sqlServerName = '${baseName}-sql-${uniqueString(resourceGroup().id)}'
var databaseName = 'Northwind'

// Get current user information for Entra ID admin
// Note: This will be set by the deployment script
@description('Azure AD administrator Object ID')
param adminObjectId string = ''

@description('Azure AD administrator login name')
param adminLogin string = ''

// Create Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// Create firewall rule for Azure services
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Create database
resource database 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

// Outputs
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
output managedIdentityNameForDb string = managedIdentityName
