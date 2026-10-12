#!/usr/bin/env bash
# Builds self-contained, single-file desktop executables for all supported platforms.
# Output: dist/<rid>/UrlShortener[.exe]  (no .NET install required on the target machine)
set -euo pipefail

cd "$(dirname "$0")/.."

API="src/UrlShortener.Api"
RIDS=("osx-arm64" "osx-x64" "win-x64" "linux-x64")

rm -rf dist

for RID in "${RIDS[@]}"; do
  echo ">>> Publishing $RID ..."
  dotnet publish "$API" -c Release -r "$RID" --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=none \
    -o "dist/$RID"

  # Normalize the executable name to "UrlShortener".
  if [[ "$RID" == win-* ]]; then
    mv "dist/$RID/UrlShortener.Api.exe" "dist/$RID/UrlShortener.exe"
  else
    mv "dist/$RID/UrlShortener.Api" "dist/$RID/UrlShortener"
    chmod +x "dist/$RID/UrlShortener"
  fi
  echo ">>> Done: dist/$RID/"
done

cat > dist/README.md <<'EOF'
# URL Shortener — Desktop Executables

Self-contained, single-file builds. **No .NET installation required.** Each runs with zero
infrastructure (SQLite + in-memory cache/events) and auto-opens your browser to the UI at
http://localhost:5080.

| Platform | Run |
|----------|-----|
| macOS (Apple Silicon) | `osx-arm64/UrlShortener` |
| macOS (Intel) | `osx-x64/UrlShortener` |
| Windows x64 | `win-x64/UrlShortener.exe` (double-click) |
| Linux x64 | `linux-x64/UrlShortener` |

## Notes
- **macOS:** unsigned, so Gatekeeper may block it. Right-click → Open, or
  `xattr -d com.apple.quarantine ./UrlShortener` then run it.
- **Windows:** SmartScreen may warn — More info → Run anyway.
- **Linux:** `chmod +x ./UrlShortener && ./UrlShortener`.
- Data is stored per-user (e.g. `~/Library/Application Support/UrlShortener/` on macOS,
  `%LOCALAPPDATA%\UrlShortener\` on Windows, `~/.local/share/UrlShortener/` on Linux).

See the repository `docs/RUNNING.md` for full details.
EOF

echo
echo "All executables built under dist/. See dist/README.md for how to run each."
