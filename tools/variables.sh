#!/bin/bash

random=49152
location="swedencentral"

resource_group_name="rg-conferencehub"
app_service_plan_name="plan-conferencehub"
app_service_plan_sku="P0V3"

web_app_name="app-conferencehub-${random}"
web_runtime="DOTNETCORE:9.0"

# LP2 variables (already in use from previous learning path)
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

# LP3 variables for session slide storage
slides_storage_account_name="stslideshub${random}"
slides_container_name="session-slides"
slides_storage_sku="Standard_LRS"
