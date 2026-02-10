#!/bin/bash

# Shared variables for learning paths.

random=49152
location="swedencentral"

resource_group_name="rg-conferencehub"
app_service_plan_name="plan-conferencehub"
app_service_plan_sku="P0V3"

web_app_name="app-conferencehub-${random}"
web_runtime="DOTNETCORE:9.0"

storage_account_name="stconferencehub${random}"
function_app_name="func-conferencehub-${random}"
function_runtime="dotnet-isolated"
function_runtime_version="9"
function_worker_runtime="dotnet-isolated"
function_key_name="default"

confirmation_sender_email="noreply@conferencehub.local"

functions_project_name="ConferenceHub.Functions"
functions_project_dir="LearningPath/02-Functions/ConferenceHub.Functions"
functions_publish_dir="LearningPath/02-Functions/ConferenceHub.Functions/publish"
functions_package_path="LearningPath/02-Functions/ConferenceHub.Functions.zip"
log_analytics_workspace_name="law-conferencehub-${random}"
function_app_insights_name="appi-conferencehub-func-${random}"
