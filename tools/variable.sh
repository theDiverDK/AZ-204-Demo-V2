#!/bin/bash

# Shared variables for all learning paths.
# Add new variables here as new learning paths are implemented.

random=49152
location="swedencentral"

resource_group_name="rg-conferencehub"
app_service_plan_name="plan-conferencehub"
app_service_plan_sku="P0V3"

web_app_name="app-conferencehub-${random}"
runtime="DOTNETCORE:9.0"

storage_account_name="stconferencehub${random}"
function_app_name="func-conferencehub-${random}"
function_runtime="node"
function_runtime_version="20"
