#!/bin/sh
rm -rf Deploy

mkdir Deploy
mkdir Deploy/Common
cp IndieLauncher/bin/Release/IndieLauncher.exe Deploy/Common

mkdir Deploy/OSX
cd Deploy/OSX
macpack -n "Example Game Launcher" -m cocoa -i ../../ExampleGame/Icon.icns ../Common/IndieLauncher.exe
