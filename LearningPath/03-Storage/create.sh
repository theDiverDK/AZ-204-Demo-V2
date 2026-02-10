#!/bin/bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
source "$repo_root/tools/variables.sh"

project_dir="$repo_root/ConferenceHub"
publish_dir="$project_dir/publish"
package_path="$project_dir/app.zip"

slides_storage_key=""
slides_storage_connection_string=""

# LP3 assumes LP1 and LP2 are already completed.
# Create only new Storage resources for session slide upload.
az storage account create \
  --name "$slides_storage_account_name" \
  --resource-group "$resource_group_name" \
  --location "$location" \
  --sku "$slides_storage_sku" \
  --kind StorageV2 \
  --allow-blob-public-access true \
  --min-tls-version TLS1_2

az storage account update \
  --name "$slides_storage_account_name" \
  --resource-group "$resource_group_name" \
  --allow-blob-public-access true

slides_storage_key="$(az storage account keys list \
  --resource-group "$resource_group_name" \
  --account-name "$slides_storage_account_name" \
  --query "[0].value" \
  -o tsv)"

az storage container create \
  --name "$slides_container_name" \
  --account-name "$slides_storage_account_name" \
  --account-key "$slides_storage_key" \
  --public-access blob

container_exists="$(az storage container exists \
  --name "$slides_container_name" \
  --account-name "$slides_storage_account_name" \
  --account-key "$slides_storage_key" \
  --query "exists" \
  -o tsv)"

if [[ "$container_exists" != "true" ]]; then
  echo "ERROR: Storage container '$slides_container_name' was not created in account '$slides_storage_account_name'."
  exit 1
fi

echo "Storage container '$slides_container_name' is ready in account '$slides_storage_account_name'."

slides_storage_connection_string="DefaultEndpointsProtocol=https;AccountName=${slides_storage_account_name};AccountKey=${slides_storage_key};EndpointSuffix=core.windows.net"

az webapp config appsettings set \
  --resource-group "$resource_group_name" \
  --name "$web_app_name" \
  --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  WEBSITE_RUN_FROM_PACKAGE=1 \
  SlideStorage__ConnectionString="$slides_storage_connection_string" \
  SlideStorage__ContainerName="$slides_container_name"

# --------------------

dotnet publish "$project_dir/ConferenceHub.csproj" -c Release -o "$publish_dir"

rm -f "$package_path"
(cd "$publish_dir" && zip -qr "$package_path" .)

az webapp deploy \
  --resource-group "$resource_group_name" \
  --name "$web_app_name" \
  --src-path "$package_path" \
  --type zip

if [[ "-e" != "1" ]]; then
az webapp browse \
  --resource-group "$resource_group_name" \
  --name "$web_app_name"
fi
