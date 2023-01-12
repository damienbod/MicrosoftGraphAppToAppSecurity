
Graph permission: User.Read.All
Type: Application
```json
"resourceAppId": "00000003-0000-0000-c000-000000000000",
"resourceAccess": [
	{
		"id": "df021288-bdef-4463-88db-98f22de89214",
		"type": "Role"
	}
]
```

```powershell
Install-Module AzureAD -AllowClobber
```

$DisplayNameServicePrincpal = the name of your Azure App service

```powershell
$TenantID = "7ff95b15-dc21-4ba6-bc92-824856578fc1"
$DisplayNameServicePrincpal ="GraphManagedIdentity20230112134834"
$GraphAppId = "00000003-0000-0000-c000-000000000000"
$PermissionName = "User.Read.All"

Connect-AzureAD -TenantId $TenantID

$sp = (Get-AzureADServicePrincipal -Filter "displayName eq '$DisplayNameServicePrincpal'")

Write-Host $sp

$GraphServicePrincipal = Get-AzureADServicePrincipal -Filter "appId eq '$GraphAppId'"

$AppRole = $GraphServicePrincipal.AppRoles | Where-Object {$_.Value -eq $PermissionName -and $_.AllowedMemberTypes -contains "Application"}

New-AzureAdServiceAppRoleAssignment -ObjectId $sp.ObjectId -PrincipalId $sp.ObjectId -ResourceId $GraphServicePrincipal.ObjectId -Id $AppRole.Id

```

The script can be validated in the Enterprise applications blade and then the permissions


