#!/bin/bash
set -e

export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer
export PATH="$DEVELOPER_DIR/usr/bin:$PATH"

APP="/Users/venkym/Downloads/Parking/ParkingRush/Builds/Mac/ParkingRush.app"
PKG="/Users/venkym/Downloads/Parking/ParkingRush/Builds/Mac/ParkingRush.pkg"
ENTITLEMENTS="/Users/venkym/Downloads/Parking/ParkingRush/Entitlements.entitlements"
BUNDLE_ID="com.parkingrush.vmullapudiapps"

# Find signing identities
APP_CERT=$(security find-identity -v -p codesigning | grep "3rd Party Mac Developer Application" | head -1 | awk -F'"' '{print $2}')
INST_CERT=$(security find-identity -v -p codesigning | grep "3rd Party Mac Developer Installer" | head -1 | awk -F'"' '{print $2}')

if [ -z "$APP_CERT" ]; then
    echo "ERROR: No 'Mac App Distribution' certificate found. Create one at developer.apple.com first."
    exit 1
fi

echo "Using App cert: $APP_CERT"
echo "Using Installer cert: $INST_CERT"

# 1. Re-sign all nested dylibs/frameworks
echo "Signing nested binaries..."
find "$APP" -name "*.dylib" -o -name "*.framework" | while read f; do
    codesign --force --sign "$APP_CERT" --entitlements "$ENTITLEMENTS" --timestamp "$f" 2>/dev/null || true
done

# 2. Sign the main .app
echo "Signing .app..."
codesign --force --deep --sign "$APP_CERT" \
    --entitlements "$ENTITLEMENTS" \
    --options runtime \
    --timestamp \
    "$APP"

echo "Verifying signature..."
codesign --verify --deep --strict "$APP"
spctl --assess --type execute "$APP" 2>&1 || true

# 3. Package as .pkg for Mac App Store
echo "Creating installer package..."
productbuild \
    --component "$APP" /Applications \
    --sign "$INST_CERT" \
    "$PKG"

echo ""
echo "✅ Done! Package at: $PKG"
echo ""
echo "Upload with:"
echo "  xcrun altool --upload-app -f '$PKG' -t osx --apiKey YOUR_KEY --apiIssuer YOUR_ISSUER"
