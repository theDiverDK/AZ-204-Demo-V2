#!/bin/bash
set -euo pipefail

log() {
  printf '[%s] %s\n' "$(date '+%H:%M:%S')" "$*"
}

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

require_base_tools() {
  require_command az
  require_command dotnet
  require_command zip
}

require_az_login() {
  az account show >/dev/null 2>&1 || fail "Azure CLI is not logged in. Run: az login"
}

ensure_resource_group() {
  local rg="$1"
  local location="$2"
  local exists
  exists="$(az group exists --name "$rg" -o tsv)"
  if [[ "$exists" == "true" ]]; then
    log "Resource group exists: $rg"
  else
    log "Creating resource group: $rg"
    az group create --name "$rg" --location "$location" >/dev/null
  fi
}

ensure_app_service_plan() {
  local plan="$1"
  local rg="$2"
  local location="$3"
  local sku="$4"
  if az appservice plan show --name "$plan" --resource-group "$rg" >/dev/null 2>&1; then
    log "App Service plan exists: $plan"
  else
    log "Creating App Service plan: $plan"
    az appservice plan create \
      --name "$plan" \
      --resource-group "$rg" \
      --location "$location" \
      --is-linux \
      --sku "$sku" >/dev/null
  fi
}

ensure_webapp() {
  local app="$1"
  local rg="$2"
  local plan="$3"
  local runtime="$4"
  if az webapp show --name "$app" --resource-group "$rg" >/dev/null 2>&1; then
    log "Web App exists: $app"
  else
    log "Creating Web App: $app"
    az webapp create \
      --name "$app" \
      --resource-group "$rg" \
      --plan "$plan" \
      --runtime "$runtime" >/dev/null
  fi
}

ensure_storage_account() {
  local storage="$1"
  local rg="$2"
  local location="$3"
  local sku="$4"
  if az storage account show --name "$storage" --resource-group "$rg" >/dev/null 2>&1; then
    log "Storage account exists: $storage"
  else
    log "Creating storage account: $storage"
    az storage account create \
      --name "$storage" \
      --resource-group "$rg" \
      --location "$location" \
      --sku "$sku" \
      --kind StorageV2 >/dev/null
  fi
}

ensure_function_app() {
  local app="$1"
  local rg="$2"
  local location="$3"
  local storage="$4"
  local runtime="$5"
  local runtime_version="$6"
  if az functionapp show --name "$app" --resource-group "$rg" >/dev/null 2>&1; then
    log "Function App exists: $app"
  else
    log "Creating Function App: $app"
    az functionapp create \
      --name "$app" \
      --resource-group "$rg" \
      --consumption-plan-location "$location" \
      --storage-account "$storage" \
      --functions-version 4 \
      --runtime "$runtime" \
      --runtime-version "$runtime_version" \
      --os-type Linux >/dev/null
  fi
}

set_webapp_settings() {
  local rg="$1"
  local app="$2"
  shift 2
  az webapp config appsettings set --resource-group "$rg" --name "$app" --settings "$@" >/dev/null
}

set_functionapp_settings() {
  local rg="$1"
  local app="$2"
  shift 2
  az functionapp config appsettings set --resource-group "$rg" --name "$app" --settings "$@" >/dev/null
}

publish_conferencehub() {
  local project_dir="$1"
  local publish_dir="$2"
  log "Publishing ConferenceHub"
  dotnet publish "$project_dir/ConferenceHub.csproj" -c Release -o "$publish_dir"
}

zip_directory() {
  local source_dir="$1"
  local package_path="$2"
  rm -f "$package_path"
  (cd "$source_dir" && zip -qr "$package_path" .)
}

deploy_webapp_zip() {
  local rg="$1"
  local app="$2"
  local package_path="$3"
  log "Deploying Web App package"
  az webapp deploy --resource-group "$rg" --name "$app" --src-path "$package_path" --type zip >/dev/null
}

deploy_functionapp_zip() {
  local rg="$1"
  local app="$2"
  local package_path="$3"
  log "Deploying Function App package"
  az functionapp deployment source config-zip --resource-group "$rg" --name "$app" --src "$package_path" >/dev/null
}

browse_webapp() {
  local rg="$1"
  local app="$2"
  az webapp browse --resource-group "$rg" --name "$app"
}
