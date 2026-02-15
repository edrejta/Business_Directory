#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

"${SCRIPT_DIR}/dotnet8.sh" clean "${SCRIPT_DIR}/../BusinessDirectory.sln" -v minimal
