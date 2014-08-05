#!/bin/sh
rm -rf Deploy
mkdir Deploy
mkdir Deploy/OSX
cp IndieLauncher/bin/Release/IndieLauncher.exe Deploy/OSX
cd Deploy/OSX
macpack -n "Example Launcher" -m cocoa -i ../../Icon.icns IndieLauncher.exe
