#!/bin/bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$repo_root"

branch="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || true)"
if [[ -z "$branch" || "$branch" == "HEAD" ]]; then
  echo "Could not detect current branch. Checkout an lp/* branch first." >&2
  exit 1
fi

if [[ "$branch" == lp/* ]]; then
  lp_key="${branch#lp/}"
elif [[ -n "${LP_PATH:-}" ]]; then
  lp_key="$LP_PATH"
else
  echo "Current branch is '$branch'. Use an lp/* branch (for example lp/01-init), or set LP_PATH." >&2
  exit 1
fi

to_folder_name() {
  local key="$1"
  local out=""
  IFS='-' read -r -a parts <<< "$key"
  for i in "${!parts[@]}"; do
    part="${parts[$i]}"
    if [[ "$i" -eq 0 ]]; then
      out="$part"
    else
      out+="-${part^}"
    fi
  done
  echo "$out"
}

lp_folder="$(to_folder_name "$lp_key")"
lp_dir="LearningPath/${lp_folder}"

if [[ ! -d "$lp_dir" ]]; then
  echo "Learning path folder not found for branch '$branch': $lp_dir" >&2
  exit 1
fi

if [[ -f "$lp_dir/lp.env" ]]; then
  set -a
  # shellcheck source=/dev/null
  source "$lp_dir/lp.env"
  set +a
fi

if [[ ! -x "$lp_dir/create.sh" ]]; then
  echo "Expected executable script: $lp_dir/create.sh" >&2
  exit 1
fi

exec "$lp_dir/create.sh"
