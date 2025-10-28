#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/Penumbra.csproj"
CONFIGURATION="${CONFIGURATION:-Release}"
BEPINEX_PLUGIN_DIR="${BEPINEX_PLUGIN_DIR:-/workspace/plugins}"
LOCAL_DOTNET_DIR="$REPO_ROOT/.dotnet"

DOTNET_CHANNELS_DEFAULT="8.0 6.0"
if [ -n "${DOTNET_CHANNEL:-}" ] && [ -z "${DOTNET_CHANNELS:-}" ]; then
    DOTNET_CHANNELS="$DOTNET_CHANNEL"
fi
DOTNET_CHANNELS="${DOTNET_CHANNELS:-$DOTNET_CHANNELS_DEFAULT}"

escape_for_regex() {
    printf '%s' "$1" | sed 's/[][().*^$?+{}|\\]/\\&/g'
}

has_required_sdks() {
    local dotnet_cli="$1"
    shift

    local installed_sdks
    installed_sdks=$("$dotnet_cli" --list-sdks 2>/dev/null | awk '{print $1}')

    for channel in "$@"; do
        local channel_pattern
        channel_pattern="^$(escape_for_regex "$channel")(\\.|$)"

        if ! grep -Eq "$channel_pattern" <<<"$installed_sdks"; then
            return 1
        fi
    done

    return 0
}

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
    # shellcheck disable=SC2206
    local required_channels=($DOTNET_CHANNELS)

    if command -v dotnet >/dev/null 2>&1 && has_required_sdks "$(command -v dotnet)" "${required_channels[@]}"; then
        return
    fi

    if [ -x "$LOCAL_DOTNET_DIR/dotnet" ]; then
        export DOTNET_ROOT="$LOCAL_DOTNET_DIR"
        export PATH="$DOTNET_ROOT:$PATH"

        if has_required_sdks "$LOCAL_DOTNET_DIR/dotnet" "${required_channels[@]}"; then
            return
        fi
    fi

    mkdir -p "$LOCAL_DOTNET_DIR"
    local installer="$LOCAL_DOTNET_DIR/dotnet-install.sh"

    if [ ! -f "$installer" ]; then
        download_dotnet_install_script "$installer"
        chmod +x "$installer"
    fi

    for channel in "${required_channels[@]}"; do
        "$installer" --channel "$channel" --install-dir "$LOCAL_DOTNET_DIR" --skip-non-versioned-files
    done

    export DOTNET_ROOT="$LOCAL_DOTNET_DIR"
    export PATH="$DOTNET_ROOT:$PATH"

    if ! command -v dotnet >/dev/null 2>&1; then
        echo "Failed to install the .NET SDK. Please install it manually." >&2
        exit 1
    fi

    if ! has_required_sdks "$(command -v dotnet)" "${required_channels[@]}"; then
        echo "The installed .NET SDK does not include the required channels: ${DOTNET_CHANNELS}." >&2
        exit 1
    fi
}

ensure_dotnet

mkdir -p "$BEPINEX_PLUGIN_DIR"

dotnet restore "$PROJECT"
dotnet build "$PROJECT" --configuration "$CONFIGURATION" -p:RunGenerateREADME=false

DLL_PATH="$REPO_ROOT/bin/$CONFIGURATION/net6.0/Penumbra.dll"

if [ ! -f "$DLL_PATH" ]; then
    echo "Build failed: $DLL_PATH not found." >&2
    exit 1
fi

cp "$DLL_PATH" "$BEPINEX_PLUGIN_DIR"
echo "Copied $(basename "$DLL_PATH") to $BEPINEX_PLUGIN_DIR"
