#!/usr/bin/env bash
# Builds the portable Docker image for the URL Shortener.
#
# Usage:
#   ./scripts/build-image.sh                      # build for THIS machine's arch and load it locally
#   ./scripts/build-image.sh tarball              # also save a loadable image tarball to dist/
#   ./scripts/build-image.sh multiarch REF        # build linux/amd64 + linux/arm64 and PUSH to REF
#                                                  #   e.g. ./scripts/build-image.sh multiarch ghcr.io/you/url-shortener:latest
#
# The image runs standalone (SQLite + in-memory cache/events) on port 8080 — no other services needed.
set -euo pipefail
cd "$(dirname "$0")/.."

IMAGE="url-shortener:latest"
MODE="${1:-local}"

case "$MODE" in
  local|tarball)
    echo ">>> Building $IMAGE for the local architecture ..."
    docker build -t "$IMAGE" .
    echo ">>> Built $(docker image inspect "$IMAGE" --format '{{.Os}}/{{.Architecture}}')"
    if [[ "$MODE" == "tarball" ]]; then
      mkdir -p dist
      ARCH=$(docker image inspect "$IMAGE" --format '{{.Architecture}}')
      OUT="dist/url-shortener-image-${ARCH}.tar.gz"
      echo ">>> Saving $OUT ..."
      docker save "$IMAGE" | gzip > "$OUT"
      echo ">>> Done. Load it elsewhere (same arch) with:  docker load -i $(basename "$OUT" .gz).gz"
    fi
    ;;
  multiarch)
    REF="${2:-}"
    if [[ -z "$REF" ]]; then
      echo "ERROR: multiarch mode needs a push reference, e.g.:"
      echo "  ./scripts/build-image.sh multiarch ghcr.io/<you>/url-shortener:latest"
      exit 1
    fi
    echo ">>> Ensuring a buildx builder exists ..."
    docker buildx create --name urlshort-builder --use >/dev/null 2>&1 || docker buildx use urlshort-builder
    docker buildx inspect --bootstrap >/dev/null
    echo ">>> Building + pushing multi-arch (linux/amd64, linux/arm64) to $REF ..."
    docker buildx build --platform linux/amd64,linux/arm64 -t "$REF" --push .
    echo ">>> Done. Run on any host with:  docker run -p 8080:8080 $REF"
    ;;
  *)
    echo "Unknown mode: $MODE (use: local | tarball | multiarch REF)"; exit 1
    ;;
esac
