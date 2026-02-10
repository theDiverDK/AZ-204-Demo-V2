#!/bin/bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
source "$repo_root/tools/variables.sh"

project_dir="$repo_root/ConferenceHub"
web_publish_dir="$project_dir/publish"
web_package_path="$project_dir/app.zip"

functions_project_path="$repo_root/$functions_project_dir/$functions_project_name.csproj"
functions_publish_path="$repo_root/$functions_publish_dir"
functions_zip_path="$repo_root/$functions_package_path"

functions_base_url="https://${function_app_name}.azurewebsites.net"
functions_send_url="${functions_base_url}/api/SendConfirmation"
function_key=""

# LP2 assumes LP1 already created RG/App Service Plan/Web App.
# Only create new Function resources, then update/deploy existing apps.
az storage account create \
  --name "$storage_account_name" \
  --resource-group "$resource_group_name" \
  --location "$location" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2

if ! az monitor log-analytics workspace show --resource-group "$resource_group_name" --workspace-name "$log_analytics_workspace_name" >/dev/null 2>&1; then
  az monitor log-analytics workspace create \
    --resource-group "$resource_group_name" \
    --workspace-name "$log_analytics_workspace_name" \
    --location "$location"
fi

workspace_id="$(az monitor log-analytics workspace show \
  --resource-group "$resource_group_name" \
  --workspace-name "$log_analytics_workspace_name" \
  --query id \
  -o tsv)"

if ! az monitor app-insights component show --app "$function_app_insights_name" --resource-group "$resource_group_name" >/dev/null 2>&1; then
  az monitor app-insights component create \
    --app "$function_app_insights_name" \
    --location "$location" \
    --resource-group "$resource_group_name" \
    --workspace "$workspace_id" \
    --application-type web
fi

az functionapp create \
  --name "$function_app_name" \
  --resource-group "$resource_group_name" \
  --consumption-plan-location "$location" \
  --storage-account "$storage_account_name" \
  --app-insights "$function_app_insights_name" \
  --functions-version 4 \
  --runtime "$function_runtime" \
  --runtime-version "$function_runtime_version"

az functionapp config set \
  --resource-group "$resource_group_name" \
  --name "$function_app_name" \
  --min-tls-version 1.2

az webapp config set \
  --resource-group "$resource_group_name" \
  --name "$web_app_name" \
  --min-tls-version 1.2

az functionapp config appsettings set \
  --resource-group "$resource_group_name" \
  --name "$function_app_name" \
  --settings \
  FUNCTIONS_WORKER_RUNTIME="$function_worker_runtime" \
  CONFIRMATION_SENDER_EMAIL="$confirmation_sender_email"

# --------------------

dotnet publish "$functions_project_path" -c Release -o "$functions_publish_path"

rm -f "$functions_zip_path"
(cd "$functions_publish_path" && zip -qr "$functions_zip_path" .)

az functionapp deployment source config-zip \
  --resource-group "$resource_group_name" \
  --name "$function_app_name" \
  --src "$functions_zip_path"

function_key="$(az functionapp keys list \
  --resource-group "$resource_group_name" \
  --name "$function_app_name" \
  --query "functionKeys.${function_key_name}" \
  -o tsv)"

az webapp config appsettings set \
  --resource-group "$resource_group_name" \
  --name "$web_app_name" \
  --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  WEBSITE_RUN_FROM_PACKAGE=1 \
  API_MODE=functions \
  FUNCTIONS_BASE_URL="$functions_base_url" \
  AzureFunctions__SendConfirmationUrl="$functions_send_url" \
  AzureFunctions__FunctionKey="$function_key"

# --------------------

dotnet publish "$project_dir/ConferenceHub.csproj" -c Release -o "$web_publish_dir"

rm -f "$web_package_path"
(cd "$web_publish_dir" && zip -qr "$web_package_path" .)

az webapp deploy \
  --resource-group "$resource_group_name" \
  --name "$web_app_name" \
  --src-path "$web_package_path" \
  --type zip

az webapp browse \
  --resource-group "$resource_group_name" \
  --name "$web_app_name"
