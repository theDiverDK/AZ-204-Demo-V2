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
runtime="${runtime:-DOTNETCORE:9.0}"

project_dir="$repo_root/ConferenceHub"
publish_dir="$project_dir/publish"
package_path="$project_dir/app.zip"

require_base_tools
require_az_login

ensure_resource_group "$resource_group_name" "$location"
ensure_app_service_plan "$app_service_plan_name" "$resource_group_name" "$location" "$app_service_plan_sku"
ensure_webapp "$web_app_name" "$resource_group_name" "$app_service_plan_name" "$runtime"

set_webapp_settings "$resource_group_name" "$web_app_name" \
  ASPNETCORE_ENVIRONMENT=Production \
  WEBSITE_RUN_FROM_PACKAGE=1 \
  API_MODE=none \
  FUNCTIONS_BASE_URL= \
  AzureFunctions__SendConfirmationUrl= \
  AzureFunctions__FunctionKey=

publish_conferencehub "$project_dir" "$publish_dir"
zip_directory "$publish_dir" "$package_path"
deploy_webapp_zip "$resource_group_name" "$web_app_name" "$package_path"

browse_webapp "$resource_group_name" "$web_app_name"
