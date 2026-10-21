targetScope = 'resourceGroup'

param name string
param location string = resourceGroup().location
param tags object = {}
param environmentId string
param userAssignedIdentityId string

@description('ACR login server. When set, the app pulls images using the user-assigned identity.')
param acrLoginServer string = ''

@description('Container image. A public placeholder is used at provision time; azd deploy pushes the real image.')
param image string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Whether ingress is reachable from the public internet.')
param external bool = true
param targetPort int = 8080
param env array = []

@description('Container app secrets referenced by env vars via secretRef.')
@secure()
param secrets object = {}
param minReplicas int = 0
param maxReplicas int = 1

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    environmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: secrets.?array ?? []
      registries: empty(acrLoginServer)
        ? []
        : [
            {
              server: acrLoginServer
              identity: userAssignedIdentityId
            }
          ]
      ingress: {
        external: external
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
      }
    }
    template: {
      containers: [
        {
          name: name
          image: image
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: env
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
