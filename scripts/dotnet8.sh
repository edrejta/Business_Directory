#!/usr/bin/env bash
set -euo pipefail

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is not installed or not on PATH" >&2
  exit 1
fi

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <dotnet-args...>" >&2
  exit 2
fi

# Pick the highest installed 8.x SDK.
SDK_VERSION=$(dotnet --list-sdks | awk '{print $1}' | grep -E '^8\.' | sort -V | tail -n 1 || true)
if [[ -z "${SDK_VERSION}" ]]; then
  echo "No .NET 8 SDK found. Install 8.x or update global.json." >&2
  exit 3
fi

DOTNET_BIN=$(command -v dotnet)
DOTNET_ROOT_GUESS=$(dirname "$(dirname "${DOTNET_BIN}")")

CANDIDATE_ROOTS=(
  "${DOTNET_ROOT_GUESS}"
  "/usr/lib64/dotnet"
  "/usr/local/share/dotnet"
  "${HOME}/.dotnet"
)

SDK_DIR=""
for ROOT in "${CANDIDATE_ROOTS[@]}"; do
  if [[ -d "${ROOT}/sdk/${SDK_VERSION}/Sdks" ]]; then
    SDK_DIR="${ROOT}/sdk/${SDK_VERSION}/Sdks"
    break
  fi
done

if [[ -z "${SDK_DIR}" ]]; then
  echo "Could not locate SDK path for ${SDK_VERSION}." >&2
  exit 4
fi

export MSBuildSDKsPath="${SDK_DIR}"

exec dotnet "$@"
