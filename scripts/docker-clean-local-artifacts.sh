#!/usr/bin/env bash
set -euo pipefail

# Clean stale PrivateCloudDrive local Docker artifacts created by temporary
# compose project names. Keeps the canonical shared image and the canonical
# pcdlocal stack intact.

CANONICAL_PROJECT="${COMPOSE_PROJECT_NAME:-pcdlocal}"
CANONICAL_IMAGE="${PCD_APP_IMAGE:-privateclouddrive/app-runtime:local}"

printf 'Canonical project: %s\n' "$CANONICAL_PROJECT"
printf 'Canonical app image: %s\n' "$CANONICAL_IMAGE"

printf '\n[1/4] Removing stale containers...\n'
mapfile -t stale_containers < <(docker ps -a --format '{{.Names}}' | grep -E '^(pcd|privateclouddrive)' | grep -v "^${CANONICAL_PROJECT}-" || true)
if ((${#stale_containers[@]})); then
  printf '%s\n' "${stale_containers[@]}"
  docker rm -f "${stale_containers[@]}"
else
  echo 'No stale containers found.'
fi

printf '\n[2/4] Removing stale project-scoped images...\n'
mapfile -t stale_images < <(docker images --format '{{.Repository}}:{{.Tag}}' | grep -E '^(pcd|privateclouddrive/)' | grep -v "^${CANONICAL_IMAGE}$" || true)
if ((${#stale_images[@]})); then
  printf '%s\n' "${stale_images[@]}"
  docker rmi -f "${stale_images[@]}"
else
  echo 'No stale images found.'
fi

printf '\n[3/4] Removing dangling images...\n'
docker image prune -f

printf '\n[4/4] Current canonical artifacts...\n'
docker ps -a --format '{{.Names}}\t{{.Image}}\t{{.Status}}' | grep -E "^${CANONICAL_PROJECT}-|\t${CANONICAL_IMAGE}\t" || true
docker images --format '{{.Repository}}:{{.Tag}}\t{{.ID}}\t{{.CreatedSince}}\t{{.Size}}' | grep -E "^${CANONICAL_IMAGE}\t" || true
