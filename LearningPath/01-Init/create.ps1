$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$PSNativeCommandUseErrorActionPreference = $true
$script_dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo_root = (Resolve-Path (Join-Path $script_dir "../..")).Path
. "$repo_root/tools/variables.ps1"
$project_dir = "$repo_root/ConferenceHub"
$publish_dir = "$project_dir/publish"
$package_path = "$project_dir/app.zip"
# --------------------
az group create --name "$resource_group_name" --location "$location"
az appservice plan create  --name "$app_service_plan_name"  --resource-group "$resource_group_name"  --location "$location"  --is-linux  --sku "$app_service_plan_sku"
az webapp create  --name "$web_app_name"  --resource-group "$resource_group_name"  --plan "$app_service_plan_name"  --runtime "$runtime"
az webapp config appsettings set  --resource-group "$resource_group_name"  --name "$web_app_name"  --settings  ASPNETCORE_ENVIRONMENT=Production  WEBSITE_RUN_FROM_PACKAGE=1
# --------------------
dotnet publish "$project_dir/ConferenceHub.csproj" -c Release -o "$publish_dir"
if (Test-Path "$package_path") { Remove-Item -Force "$package_path" }
Push-Location "$publish_dir"
try {
    if (Test-Path "$package_path") { Remove-Item -Force "$package_path" }
    Compress-Archive -Path (Join-Path "$publish_dir" '*') -DestinationPath "$package_path" -Force
} finally {
    Pop-Location
}
az webapp deploy  --resource-group "$resource_group_name"  --name "$web_app_name"  --src-path "$package_path"  --type zip
az webapp browse  --resource-group "$resource_group_name"  --name "$web_app_name"
