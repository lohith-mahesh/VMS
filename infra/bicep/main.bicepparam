using './main.bicep'

param namePrefix = 'rrvms'
param location = 'centralindia'
param environmentName = 'prod'
param sqlAdministratorName = 'RRVMS SQL Administrators'
param sqlAdministratorObjectId = '<entra-group-object-id>'
param apiClientId = '<api-app-registration-client-id>'
param frontendClientId = '<spa-app-registration-client-id>'
param graphEnabled = false
param senderMailbox = ''
param applicationTimeZoneId = 'India Standard Time'
