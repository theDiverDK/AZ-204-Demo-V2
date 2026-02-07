#!/bin/bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
  echo "Run this script from inside a git repository." >&2
  exit 1
fi
cd "$repo_root"

main_branch="main"
if ! git show-ref --verify --quiet refs/heads/${main_branch}; then
  current="$(git rev-parse --abbrev-ref HEAD)"
  git branch -m "$current" "$main_branch"
fi

branches=(
  "lp/01-init"
  "lp/02-functions"
  "lp/03-storage"
  "lp/04-cosmos"
  "lp/05-security"
  "lp/06-monitoring"
  "lp/07-api-management"
  "lp/08-messaging"
  "lp/09-caching"
  "lp/10-containers"
  "lp/11-realtime"
)

worktree_dirs=(
  "worktrees/lp01"
  "worktrees/lp02"
  "worktrees/lp03"
  "worktrees/lp04"
  "worktrees/lp05"
  "worktrees/lp06"
  "worktrees/lp07"
  "worktrees/lp08"
  "worktrees/lp09"
  "worktrees/lp10"
  "worktrees/lp11"
)

mkdir -p worktrees

for i in "${!branches[@]}"; do
  branch="${branches[$i]}"
  dir="${worktree_dirs[$i]}"

  if ! git show-ref --verify --quiet "refs/heads/${branch}"; then
    git branch "$branch" "$main_branch"
  fi

  if [[ -d "$dir/.git" || -f "$dir/.git" ]]; then
    echo "Worktree exists: $dir"
    continue
  fi

  if [[ -e "$dir" && -n "$(ls -A "$dir" 2>/dev/null || true)" ]]; then
    echo "Skipping non-empty path: $dir"
    continue
  fi

  git worktree add "$dir" "$branch"
  echo "Created worktree: $dir -> $branch"
done

git worktree list
