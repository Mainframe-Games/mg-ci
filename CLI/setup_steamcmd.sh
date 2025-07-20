#!/bin/bash

OS=$(uname)
IS_LINUX=$( [ "$OS" = "Linux" ] && echo "true" || echo "false" )
IS_MAC=$( [ "$OS" = "Darwin" ] && echo "true" || echo "false" )
IS_WINDOWS=$( [ "${OS#CYGWIN}" != "$OS" ] || [ "${OS#MINGW}" != "$OS" ] || [ "${OS#MSYS}" != "$OS" ] && echo "true" || echo "false" )

STEAMWORK_SDK_VERSION=160
STEAMWORKS_ZIP="steamworks_sdk_$STEAMWORK_SDK_VERSION.zip"
STEAMWORKS_SDK_URL=https://partner.steamgames.com/downloads/$STEAMWORKS_ZIP

echo "STEAMWORKS_SDK_URL=$STEAMWORKS_SDK_URL"

if $IS_LINUX; then
    STEAM_PATH="/home/$USER/steamcmd"
    STEAM_PATH_TEMP="/home/$USER/steamcmd_temp"
elif $IS_MAC; then
    STEAM_PATH="/Applcations/steamcmd"
    STEAM_PATH_TEMP="/Applcations/steamcmd_temp"
elif $IS_WINDOWS; then
    USER=$(whoami)
    STEAM_PATH="C:/Users/$USER/steamcmd"
    STEAM_PATH_TEMP="C:/Users/$USER/steamcmd_temp"
else
    echo "Unknown operating system."
    exit -1
fi

# remove temp if there
rm -rf $STEAM_PATH_TEMP
rm -rf $STEAM_PATH

# download
echo "Downloading Steamworks SDK..."
wget -q -P "$STEAM_PATH_TEMP" $STEAMWORKS_SDK_URL || exit

# unzup
echo "Unzipping Steamworks SDK"
unzip -o "$STEAM_PATH_TEMP/$STEAMWORKS_ZIP" -d $STEAM_PATH_TEMP || exit

# make destination folder
mkdir "$STEAM_PATH"

# move contents and set alias
if $IS_LINUX; then
    cp -r "$STEAM_PATH_TEMP/sdk/tools/ContentBuilder/builder_linux"/* "$STEAM_PATH/" || exit
elif $IS_MAC; then
    cp -r "$STEAM_PATH_TEMP/sdk/tools/ContentBuilder/builder_osx"/* "$STEAM_PATH/" || exit
elif $IS_WINDOWS; then
    cp -r "$STEAM_PATH_TEMP/sdk/tools/ContentBuilder/builder"/* "$STEAM_PATH/" || exit
fi

# remove temp
rm -rf $STEAM_PATH_TEMP