targetScope = 'subscription'

@minLength(1)
@description('Name of the azd environment; used to derive resource names and the resource group.')
param environmentName string

@minLength(1)
@description('Primary Azure region for backend + database resources.')
param location string

@description('Region for the Static Web Apps (they are not offered in every region).')
param swaLocation string = 'westeurope'

@description('Use the Azure SQL free offer (up to 10 free serverless databases per subscription).')
param useFreeSqlLimit bool = true

@description('Shared HMAC signing key for JWT access tokens. Must be identical across all services and at least 32 characters.')
@secure()
param jwtSigningKey string

@description('Bootstrap administrator email. Leave empty to skip admin seeding.')
param adminEmail string = ''

@description('Bootstrap administrator password (>= 12 chars). Leave empty to skip admin seeding.')
@secure()
param adminPassword string = ''

var suffix = take(uniqueString(subscription().id, environmentName), 6)
var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module identity 'modules/identity.bicep' = {
  name: 'identity'
  scope: rg
  params: {
    name: 'id-${environmentName}-${suffix}'
    location: location
    tags: tags
  }
}

module registry 'modules/registry.bicep' = {
  name: 'registry'
  scope: rg
  params: {
    name: toLower(replace('acr${environmentName}${suffix}', '-', ''))
    location: location
    tags: tags
  }
}

module platform 'modules/environment.bicep' = {
  name: 'environment'
  scope: rg
  params: {
    name: 'cae-${environmentName}-${suffix}'
    logAnalyticsName: 'log-${environmentName}-${suffix}'
    location: location
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    name: toLower(replace('st${environmentName}${suffix}', '-', ''))
    location: location
    tags: tags
  }
}

module blobRole 'modules/blobRole.bicep' = {
  name: 'blob-role-uami'
  scope: rg
  params: {
    storageAccountName: storage.outputs.name
    principalId: identity.outputs.principalId
  }
}

var databaseNames = [
  'AvraProductService'
  'AvraCartService'
  'AvraUserService'
  'AvraEventsService'
  'AvraCommunityService'
  'AvraWellnessService'
  'AvraOrderingService'
  'AvraEventBus'
]

module sql 'modules/sql.bicep' = {
  name: 'sql'
  scope: rg
  params: {
    name: toLower('sql-${environmentName}-${suffix}')
    location: location
    tags: tags
    adminLogin: identity.outputs.name
    adminSid: identity.outputs.principalId
    databaseNames: databaseNames
    useFreeLimit: useFreeSqlLimit
  }
}

module web 'modules/staticWebApp.bicep' = {
  name: 'web'
  scope: rg
  params: {
    name: 'stapp-web-${suffix}'
    location: swaLocation
    tags: union(tags, { 'azd-service-name': 'web' })
  }
}

module admin 'modules/staticWebApp.bicep' = {
  name: 'admin'
  scope: rg
  params: {
    name: 'stapp-admin-${suffix}'
    location: swaLocation
    tags: union(tags, { 'azd-service-name': 'admin' })
  }
}

var defaultDomain = platform.outputs.defaultDomain
var sqlFqdn = sql.outputs.fqdn
var uamiClientId = identity.outputs.clientId

var productUrl = 'https://ca-product-${suffix}.${defaultDomain}'
var userUrl = 'https://ca-user-${suffix}.${defaultDomain}'
var paymentUrl = 'https://ca-payment-${suffix}.internal.${defaultDomain}'
var webOrigin = 'https://${web.outputs.defaultHostname}'
var adminOrigin = 'https://${admin.outputs.defaultHostname}'
var eventBusConn = 'Server=tcp:${sqlFqdn},1433;Database=AvraEventBus;Authentication=Active Directory Managed Identity;User Id=${uamiClientId};Encrypt=True;TrustServerCertificate=False;'

var baseEnv = [
  { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
  { name: 'Urls', value: 'http://+:8080' }
  { name: 'AllowedHosts', value: '*' }
  { name: 'AZURE_CLIENT_ID', value: uamiClientId }
  { name: 'EventBus__ConnectionString', value: eventBusConn }
  { name: 'ConnectionStrings__EventBus', value: eventBusConn }
  { name: 'Cors__AllowedOrigins__0', value: webOrigin }
  { name: 'Cors__AllowedOrigins__1', value: adminOrigin }
  { name: 'Jwt__SigningKey', secretRef: 'jwt-signing-key' }
]

var serviceDefs = [
  { key: 'product', external: true, db: 'AvraProductService', catalog: false, users: false, payments: false, admin: false, storage: true }
  { key: 'cart', external: true, db: 'AvraCartService', catalog: true, users: true, payments: false, admin: false, storage: false }
  { key: 'user', external: true, db: 'AvraUserService', catalog: false, users: false, payments: false, admin: true, storage: false }
  { key: 'events', external: true, db: 'AvraEventsService', catalog: false, users: true, payments: true, admin: false, storage: true }
  { key: 'community', external: true, db: 'AvraCommunityService', catalog: false, users: false, payments: false, admin: false, storage: false }
  { key: 'wellness', external: true, db: 'AvraWellnessService', catalog: false, users: false, payments: false, admin: false, storage: false }
  { key: 'ordering', external: true, db: 'AvraOrderingService', catalog: false, users: false, payments: false, admin: false, storage: false }
  { key: 'payment', external: false, db: '', catalog: false, users: false, payments: false, admin: false, storage: false }
]

var seedAdmin = !empty(adminEmail) && !empty(adminPassword)

module acrPull 'modules/acrPullRole.bicep' = {
  name: 'acrpull-uami'
  scope: rg
  params: {
    acrName: registry.outputs.name
    principalId: identity.outputs.principalId
  }
}

module apps 'modules/containerApp.bicep' = [
  for svc in serviceDefs: {
    name: 'ca-${svc.key}'
    scope: rg
    params: {
      name: 'ca-${svc.key}-${suffix}'
      location: location
      tags: union(tags, { 'azd-service-name': svc.key })
      environmentId: platform.outputs.id
      userAssignedIdentityId: identity.outputs.id
      acrLoginServer: registry.outputs.loginServer
      external: svc.external
      env: concat(
        baseEnv,
        empty(svc.db)
          ? []
          : [
              {
                name: 'ConnectionStrings__DefaultConnection'
                value: 'Server=tcp:${sqlFqdn},1433;Database=${svc.db};Authentication=Active Directory Managed Identity;User Id=${uamiClientId};Encrypt=True;TrustServerCertificate=False;'
              }
            ],
        svc.catalog ? [{ name: 'Services__Catalog__BaseUrl', value: productUrl }] : [],
        svc.users ? [{ name: 'Services__Users__BaseUrl', value: userUrl }] : [],
        svc.payments ? [{ name: 'Services__Payments__BaseUrl', value: paymentUrl }] : [],
        svc.storage
          ? [
              { name: 'Storage__BlobEndpoint', value: storage.outputs.blobEndpoint }
              { name: 'Storage__Container', value: storage.outputs.containerName }
            ]
          : [],
        (svc.admin && seedAdmin)
          ? [
              { name: 'AVRA_Admin__Email', value: adminEmail }
              { name: 'AVRA_Admin__Password', secretRef: 'admin-password' }
            ]
          : []
      )
      secrets: {
        array: concat(
          [{ name: 'jwt-signing-key', value: jwtSigningKey }],
          (svc.admin && seedAdmin) ? [{ name: 'admin-password', value: adminPassword }] : []
        )
      }
    }
    dependsOn: [
      acrPull
      blobRole
    ]
  }
]

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output AZURE_SQL_SERVER string = sqlFqdn
output AZURE_STORAGE_ACCOUNT string = storage.outputs.name
output AZURE_STORAGE_CONTAINER string = storage.outputs.containerName
output VITE_PRODUCT_API_URL string = productUrl
output VITE_CART_API_URL string = 'https://ca-cart-${suffix}.${defaultDomain}'
output VITE_USER_API_URL string = userUrl
output VITE_EVENTS_API_URL string = 'https://ca-events-${suffix}.${defaultDomain}'
output VITE_COMMUNITY_API_URL string = 'https://ca-community-${suffix}.${defaultDomain}'
output VITE_WELLNESS_API_URL string = 'https://ca-wellness-${suffix}.${defaultDomain}'
output VITE_ORDERING_API_URL string = 'https://ca-ordering-${suffix}.${defaultDomain}'
output WEB_URI string = webOrigin
output ADMIN_URI string = adminOrigin
