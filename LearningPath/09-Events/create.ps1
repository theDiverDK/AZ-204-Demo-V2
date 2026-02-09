$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$PSNativeCommandUseErrorActionPreference = $true
$script_dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo_root = (Resolve-Path (Join-Path $script_dir "../..")).Path
. "$repo_root/tools/variables.ps1"
$subscription_id = ""
$key_vault_id = ""
$web_principal_id = ""
$role_assignment_id = ""
$eventhub_connection_string = ""
$storage_account_id = ""
$existing_subscription_name = ""
$function_resource_id = ""
$existing_namespace_name = ""
$existing_eventhub_name = ""
$existing_auth_rule_name = ""
$project_dir = "$repo_root/ConferenceHub"
$web_publish_dir = "$repo_root/.deploy/lp09/web/publish"
$web_package_path = "$repo_root/.deploy/lp09/web/app.zip"
$functions_project_path = "$repo_root/$functions_project_dir/$functions_project_name.csproj"
$functions_publish_path = "$repo_root/.deploy/lp09/functions/publish"
$functions_zip_path = "$repo_root/.deploy/lp09/functions/functions.zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem

function New-ZipFromDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDirectory,
        [Parameter(Mandatory = $true)][string]$DestinationZipPath
    )

    if (Test-Path -LiteralPath $DestinationZipPath) {
        Remove-Item -LiteralPath $DestinationZipPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $SourceDirectory,
        $DestinationZipPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false
    )
}
$subscription_id = "$(az account show --query id -o tsv)"
$existing_namespace_name = "$(az eventhubs namespace list  --resource-group `"$resource_group_name`"  --query `"[?name=='$eventhub_namespace_name'].name | [0]`"  -o tsv)"
if ([string]::IsNullOrEmpty($existing_namespace_name)) {
    az eventhubs namespace create  --name "$eventhub_namespace_name"  --resource-group "$resource_group_name"  --location "$location"  --sku Standard
}
$existing_eventhub_name = "$(az eventhubs eventhub list  --namespace-name `"$eventhub_namespace_name`"  --resource-group `"$resource_group_name`"  --query `"[?name=='$eventhub_name'].name | [0]`"  -o tsv)"
if ([string]::IsNullOrEmpty($existing_eventhub_name)) {
    az eventhubs eventhub create  --name "$eventhub_name"  --namespace-name "$eventhub_namespace_name"  --resource-group "$resource_group_name"
}
$existing_auth_rule_name = "$(az eventhubs namespace authorization-rule list  --namespace-name `"$eventhub_namespace_name`"  --resource-group `"$resource_group_name`"  --query `"[?name=='$eventhub_auth_rule_name'].name | [0]`"  -o tsv)"
if ([string]::IsNullOrEmpty($existing_auth_rule_name)) {
    az eventhubs namespace authorization-rule create  --name "$eventhub_auth_rule_name"  --namespace-name "$eventhub_namespace_name"  --resource-group "$resource_group_name"  --rights Send
}
$eventhub_connection_string = "$(az eventhubs namespace authorization-rule keys list  --name `"$eventhub_auth_rule_name`"  --namespace-name `"$eventhub_namespace_name`"  --resource-group `"$resource_group_name`"  --query primaryConnectionString  -o tsv)"
$key_vault_id = "$(az keyvault list  --resource-group `"$resource_group_name`"  --query `"[?name=='$key_vault_name'].id | [0]`"  -o tsv)"
if ([string]::IsNullOrEmpty($key_vault_id)) {
    Write-Host "Could not find Key Vault '$key_vault_name'. Run LP7 first."
    exit 1
}
az keyvault secret set  --vault-name "$key_vault_name"  --name "$kv_secret_eventhub_connection_string_name"  --value "$eventhub_connection_string"
$web_principal_id = "$(az webapp identity assign  --resource-group `"$resource_group_name`"  --name `"$web_app_name`"  --query principalId  -o tsv)"
$role_assignment_id = "$(az role assignment list --assignee-object-id `"$web_principal_id`" --scope `"$key_vault_id`" --role `"Key Vault Secrets User`" --query `"[0].id`" -o tsv)"
if ([string]::IsNullOrEmpty($role_assignment_id)) {
    az role assignment create  --assignee-object-id "$web_principal_id"  --assignee-principal-type ServicePrincipal  --role "Key Vault Secrets User"  --scope "$key_vault_id"
}
az webapp config appsettings set  --resource-group "$resource_group_name"  --name "$web_app_name"  --settings  EventHub__ConnectionString="@Microsoft.KeyVault(VaultName=$key_vault_name;SecretName=$kv_secret_eventhub_connection_string_name)"  EventHub__HubName="$eventhub_name"
try {
    az webapp config appsettings delete  --resource-group "$resource_group_name"  --name "$web_app_name"  --setting-names  EventHubConnectionString  EVENTHUB_CONNECTION_STRING
} catch {
}
# --------------------
if (Test-Path "$functions_publish_path") { Remove-Item -Recurse -Force "$functions_publish_path" }
New-Item -ItemType Directory -Path "$functions_publish_path" -Force | Out-Null
dotnet publish "$functions_project_path" -c Release -o "$functions_publish_path"
New-ZipFromDirectory -SourceDirectory "$functions_publish_path" -DestinationZipPath "$functions_zip_path"
az functionapp deployment source config-zip  --resource-group "$resource_group_name"  --name "$function_app_name"  --src "$functions_zip_path"

$function_name = "SlideUploadedEvent"
$function_found = $false
for ($attempt = 1; $attempt -le 18; $attempt++) {
    try {
        $null = az functionapp function show --resource-group "$resource_group_name" --name "$function_app_name" --function-name "$function_name" --query name -o tsv
        $function_found = $true
        break
    } catch {
        Write-Host "Waiting for function '$function_name' to be discoverable (attempt $attempt/18)..."
        Start-Sleep -Seconds 10
    }
}
if (-not $function_found) {
    throw "Function '$function_name' was not found in '$function_app_name' after deployment."
}

$function_resource_id = "/subscriptions/$subscription_id/resourceGroups/$resource_group_name/providers/Microsoft.Web/sites/$function_app_name/functions/$function_name"
$storage_account_id = "$(az storage account show --name `"$slides_storage_account_name`" --resource-group `"$resource_group_name`" --query id -o tsv)"
$existing_subscription_name = "$(az eventgrid event-subscription list  --source-resource-id `"$storage_account_id`"  --query `"[?name=='$eventgrid_subscription_name'].name | [0]`"  -o tsv)"
if ([string]::IsNullOrEmpty($existing_subscription_name)) {
    az eventgrid event-subscription create  --name "$eventgrid_subscription_name"  --source-resource-id "$storage_account_id"  --included-event-types Microsoft.Storage.BlobCreated  --subject-begins-with "/blobServices/default/containers/$slides_container_name/blobs/"  --endpoint-type azurefunction  --endpoint "$function_resource_id"
}
if (Test-Path "$web_publish_dir") { Remove-Item -Recurse -Force "$web_publish_dir" }
New-Item -ItemType Directory -Path "$web_publish_dir" -Force | Out-Null
dotnet publish "$project_dir/ConferenceHub.csproj" -c Release -o "$web_publish_dir"
New-ZipFromDirectory -SourceDirectory "$web_publish_dir" -DestinationZipPath "$web_package_path"
az webapp deploy  --resource-group "$resource_group_name"  --name "$web_app_name"  --src-path "$web_package_path"  --type zip
az webapp restart  --resource-group "$resource_group_name"  --name "$web_app_name"
az webapp browse  --resource-group "$resource_group_name"  --name "$web_app_name"
