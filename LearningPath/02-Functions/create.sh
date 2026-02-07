#!/bin/bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
# shellcheck source=/dev/null
source "$repo_root/tools/common.sh"

random="${random:-49152}"
location="${location:-swedencentral}"
resource_group_name="${resource_group_name:-rg-conferencehub}"
app_service_plan_name="${app_service_plan_name:-plan-conferencehub}"
web_app_name="${web_app_name:-app-conferencehub-${random}}"
app_service_plan_sku="${app_service_plan_sku:-P0V3}"
web_runtime="${web_runtime:-DOTNETCORE:9.0}"
storage_account_name="${storage_account_name:-stconferencehub${random}}"
function_app_name="${function_app_name:-func-conferencehub-${random}}"
function_runtime="${function_runtime:-node}"
function_runtime_version="${function_runtime_version:-20}"

project_dir="$repo_root/ConferenceHub"
publish_dir="$project_dir/publish"
web_package_path="$project_dir/app.zip"
functions_dir="$script_dir/functions"
functions_package_path="$script_dir/functions.zip"
functions_base_url="https://${function_app_name}.azurewebsites.net"
functions_send_url="${functions_base_url}/api/SendConfirmation"

require_base_tools
require_az_login

ensure_resource_group "$resource_group_name" "$location"
ensure_app_service_plan "$app_service_plan_name" "$resource_group_name" "$location" "$app_service_plan_sku"
ensure_webapp "$web_app_name" "$resource_group_name" "$app_service_plan_name" "$web_runtime"
ensure_storage_account "$storage_account_name" "$resource_group_name" "$location" Standard_LRS
ensure_function_app "$function_app_name" "$resource_group_name" "$location" "$storage_account_name" "$function_runtime" "$function_runtime_version"

set_functionapp_settings "$resource_group_name" "$function_app_name" \
  AzureWebJobsFeatureFlags=EnableWorkerIndexing

zip_directory "$functions_dir" "$functions_package_path"
deploy_functionapp_zip "$resource_group_name" "$function_app_name" "$functions_package_path"

set_webapp_settings "$resource_group_name" "$web_app_name" \
  ASPNETCORE_ENVIRONMENT=Production \
  WEBSITE_RUN_FROM_PACKAGE=1 \
  API_MODE=functions \
  FUNCTIONS_BASE_URL="$functions_base_url" \
  AzureFunctions__SendConfirmationUrl="$functions_send_url" \
  AzureFunctions__FunctionKey=

publish_conferencehub "$project_dir" "$publish_dir"
zip_directory "$publish_dir" "$web_package_path"
deploy_webapp_zip "$resource_group_name" "$web_app_name" "$web_package_path"

browse_webapp "$resource_group_name" "$web_app_name"
