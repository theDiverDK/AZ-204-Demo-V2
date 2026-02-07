# AZ-204 Demo with Git Worktrees

This repository keeps `ConferenceHub/` as the shared web app source of truth on `main`.
Learning path differences are implemented under `LearningPath/<NN-Name>/` and selected by branch.

## Branch and Folder Convention
- Branches: `lp/01-init` ... `lp/11-realtime`
- Folders: `LearningPath/01-Init` ... `LearningPath/11-Realtime`

## Create Worktrees
From repo root:

```bash
./tools/worktrees.sh
```

This creates local worktrees under `./worktrees/lp01` ... `./worktrees/lp11`.

## Run a Learning Path
From a worktree (or any checkout on an `lp/*` branch):

```bash
./create.sh
```

`./create.sh` detects the current branch, loads `LearningPath/<NN-Name>/lp.env` if present, and executes `LearningPath/<NN-Name>/create.sh`.

## Learning Path 2 (Functions)
`LearningPath/02-Functions/create.sh` provisions and deploys:
- Resource Group
- App Service Plan + Web App for ConferenceHub
- Storage Account
- Function App (Node.js v4)
- Function code under `LearningPath/02-Functions/functions/`

It then sets Web App app settings:
- `API_MODE=functions`
- `FUNCTIONS_BASE_URL=https://<functionapp>.azurewebsites.net`
- `AzureFunctions__SendConfirmationUrl=<base>/api/SendConfirmation`

## Merge Main into Learning Path Branches
For each branch:

```bash
git checkout lp/02-functions
git merge main
```

Or update all worktrees by repeating merge in each `worktrees/lpXX` directory.
