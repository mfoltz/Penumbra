#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/Penumbra.csproj"
CONFIGURATION="Release"
BEPINEX_PLUGIN_DIR="${BEPINEX_PLUGIN_DIR:-/workspace/plugins}"
LOCAL_DOTNET_DIR="$SCRIPT_DIR/.dotnet"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-8.0}"

download_dotnet_install_script() {
    local destination="$1"
    local url="https://dot.net/v1/dotnet-install.sh"

    if command -v curl >/dev/null 2>&1; then
        curl -sSL "$url" -o "$destination"
    elif command -v wget >/dev/null 2>&1; then
        wget -qO "$destination" "$url"
    else
        echo "Neither curl nor wget is available to download the .NET installer." >&2
        return 1
    fi
}

ensure_dotnet() {
    if command -v dotnet >/dev/null 2>&1; then
        return
    fi

    if [ -x "$LOCAL_DOTNET_DIR/dotnet" ]; then
        export DOTNET_ROOT="$LOCAL_DOTNET_DIR"
        export PATH="$DOTNET_ROOT:$PATH"

        local channel_pattern
        channel_pattern="$(printf '%s' "$DOTNET_CHANNEL" | sed 's/[][().*^$?+{}|\\]/\\&/g')"

        if "$LOCAL_DOTNET_DIR/dotnet" --list-sdks | awk '{print $1}' | grep -Eq "^${channel_pattern}(\\.|$)"; then
            return
        fi
    fi

    mkdir -p "$LOCAL_DOTNET_DIR"
    local installer="$LOCAL_DOTNET_DIR/dotnet-install.sh"

    if [ ! -f "$installer" ]; then
        download_dotnet_install_script "$installer"
        chmod +x "$installer"
    fi

    "$installer" --channel "$DOTNET_CHANNEL" --install-dir "$LOCAL_DOTNET_DIR" --skip-non-versioned-files

    export DOTNET_ROOT="$LOCAL_DOTNET_DIR"
    export PATH="$DOTNET_ROOT:$PATH"

    if ! command -v dotnet >/dev/null 2>&1; then
        echo "Failed to install the .NET SDK. Please install it manually." >&2
        exit 1
    fi
}

ensure_dotnet

mkdir -p "$BEPINEX_PLUGIN_DIR"

dotnet restore "$PROJECT"
dotnet build "$PROJECT" --configuration "$CONFIGURATION" -p:RunGenerateREADME=false

DLL_PATH="$SCRIPT_DIR/bin/$CONFIGURATION/net6.0/Penumbra.dll"

if [ ! -f "$DLL_PATH" ]; then
    echo "Build failed: $DLL_PATH not found." >&2
    exit 1
fi

cp "$DLL_PATH" "$BEPINEX_PLUGIN_DIR"
echo "Copied $(basename "$DLL_PATH") to $BEPINEX_PLUGIN_DIR"
