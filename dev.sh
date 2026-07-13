#!/usr/bin/env bash
set -e
[ -x "$HOME/.dotnet/dotnet" ] && export PATH="$HOME/.dotnet:$PATH"
if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet SDK not found. Install .NET 9 with:" >&2
    echo "    curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir \$HOME/.dotnet" >&2
    exit 1
fi
exec dotnet run --project src/FtdModularLabs.App/FtdModularLabs.App -f net9.0-desktop "$@"
