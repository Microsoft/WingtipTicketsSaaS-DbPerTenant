<#
.SYNOPSIS
  Resets tenant databases that have not been changed in the recovery region to the original Wingtip region

.DESCRIPTION
  This script is intended to be run as a background job in the 'Repatriate-IntoOriginalRegion' script that repatriates the Wingtip SaaS app environment (apps, databases, servers e.t.c) into the origin.
  The script resets tenant databases tha have not been changed in the recovery region to the original Wingtip region

.PARAMETER WingtipRecoveryResourceGroup
  Resource group in the recovery region that contains recovered resources

.EXAMPLE
  [PS] C:\>.\Reset-UnchangedDatabases.ps1 -WingtipRecoveryResourceGroup "sampleRecoveryResourceGroup"
#>
[cmdletbinding()]
param (
    [parameter(Mandatory=$true)]
    [String] $WingtipRecoveryResourceGroup 
)

Import-Module "$using:scriptPath\..\..\Common\CatalogAndDatabaseManagement" -Force
Import-Module "$using:scriptPath\..\..\WtpConfig" -Force
Import-Module "$using:scriptPath\..\..\UserConfig" -Force

# Stop execution on error 
$ErrorActionPreference = "Stop"
  
# Login to Azure subscription
$credentialLoad = Import-AzureRmContext -Path "$env:TEMP\profile.json"
if (!$credentialLoad)
{
    Initialize-Subscription
}

# Get deployment configuration  
$wtpUser = Get-UserConfig
$config = Get-Configuration
$sleepInterval = 10

# Get the tenant catalog in the recovery region
$tenantCatalog = Get-Catalog -ResourceGroupName $WingtipRecoveryResourceGroup -WtpUser $wtpUser.Name

# Get list of tenants that have unchanged databases
$unchangedTenantDatabases = @()
$resetDatabaseCount = 0
$tenantList = Get-ExtendedTenant -Catalog $tenantCatalog
foreach ($tenant in $tenantList)
{
  $tenantDataChanged = Test-IfTenantDataChanged -Catalog $tenantCatalog -TenantName $tenant.TenantName
  if (!$tenantDataChanged)
  {
    $unchangedTenantDatabases += $tenant
  }
}

# Output recovery progress 
$unchangedDatabaseCount = $unchangedTenantDatabases.length 
$DatabaseRecoveryPercentage = [math]::Round($resetDatabaseCount/$unchangedDatabaseCount,2)
$DatabaseRecoveryPercentage = $DatabaseRecoveryPercentage * 100
Write-Output "$DatabaseRecoveryPercentage% ($($resetDatabaseCount) of $unchangedDatabaseCount)"

# Reset unchanged tenant databases
foreach ($tenant in $unchangedTenantDatabases)
{
  $tenantKey = Get-TenantKey $tenant.TenantName
  $originTenantServerName = ($tenant.ServerName -split "$($config.RecoveryRoleSuffix)")[0]

  $dbState = Update-TenantResourceRecoveryState -Catalog $tenantCatalog -UpdateAction "startReset" -ServerName $tenant.ServerName -DatabaseName $tenant.DatabaseName
  $tenantState = Update-TenantRecoveryState -Catalog $tenantCatalog -UpdateAction "startReset" -TenantKey $tenantKey

  # Update tenant resources to origin region
  $tenantReset = Update-TenantShardInfo -Catalog $tenantCatalog -TenantName $tenant.TenantName -FullyQualifiedTenantServerName "$originTenantServerName.database.windows.net" -TenantDatabaseName $tenant.DatabaseName
  if ($tenantReset)
  {
    $dbState = Update-TenantResourceRecoveryState -Catalog $tenantCatalog -UpdateAction "endReset" -ServerName $originTenantServerName -DatabaseName $tenant.DatabaseName
    $tenantState = Update-TenantRecoveryState -Catalog $tenantCatalog -UpdateAction "endReset" -TenantKey $tenantKey
    $resetDatabaseCount+=1
  }
  else
  {
    $dbState = Update-TenantResourceRecoveryState -Catalog $tenantCatalog -UpdateAction "markError" -ServerName $tenant.ServerName -DatabaseName $tenant.DatabaseName
  }

  # Output recovery progress 
  $DatabaseRecoveryPercentage = [math]::Round($resetDatabaseCount/$unchangedDatabaseCount,2)
  $DatabaseRecoveryPercentage = $DatabaseRecoveryPercentage * 100
  Write-Output "$DatabaseRecoveryPercentage% ($($resetDatabaseCount) of $unchangedDatabaseCount)"
}

# Output recovery progress 
$DatabaseRecoveryPercentage = [math]::Round($resetDatabaseCount/$unchangedDatabaseCount,2)
$DatabaseRecoveryPercentage = $DatabaseRecoveryPercentage * 100
Write-Output "$DatabaseRecoveryPercentage% ($($resetDatabaseCount) of $unchangedDatabaseCount)"

