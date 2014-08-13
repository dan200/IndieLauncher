#!/bin/sh

# Settings
GAME_TITLE=ExampleGame
GAME_LONG_TITLE="Example Game"

# Common
rm -rf Deploy
mkdir Deploy
mkdir Deploy/Common
if [ -f IndieLauncher/bin/Release/IndieLauncher.exe ]
then
    cp IndieLauncher/bin/Release/IndieLauncher.exe Deploy/Common/${GAME_TITLE}.exe
else
    echo "IndieLauncher.exe not found. Build the solution in Release mode first."
    exit
fi

###########
# WINDOWS #
###########
echo "Creating Windows Deployment"
rm -rf Deploy/Windows
rm -rf Deploy/${GAME_TITLE}_Windows.zip
mkdir -p Deploy/Windows
cd Deploy/Windows
cp ../Common/${GAME_TITLE}.exe "${GAME_LONG_TITLE} Launcher.exe"
zip -rq ../${GAME_TITLE}_Windows.zip .
cd ../..

#######
# OSX #
#######
echo "Creating OSX Deployment"
rm -rf Deploy/OSX
rm -rf Deploy/${GAME_TITLE}_OSX.zip
mkdir -p Deploy/OSX
cd Deploy/OSX
macpack -n "${GAME_TITLE}Launcher" -i ../../Icon.icns -m cocoa ../Common/${GAME_TITLE}.exe
cat "${GAME_TITLE}Launcher.app/Contents/MacOS/${GAME_TITLE}Launcher" | head -n 9 > temp
echo "# Make GTK# work" >> temp
echo "export DYLD_FALLBACK_LIBRARY_PATH=\"/Library/Frameworks/Mono.framework/Versions/Current/lib:\${DYLD_FALLBACK_LIBRARY_PATH}:/usr/lib\"" >> temp
echo "" >> temp
cat "${GAME_TITLE}Launcher.app/Contents/MacOS/${GAME_TITLE}Launcher" | tail -n +10 >> temp
cat temp > "${GAME_TITLE}Launcher.app/Contents/MacOS/${GAME_TITLE}Launcher"
rm -f temp
mv "${GAME_TITLE}Launcher.app" "${GAME_LONG_TITLE} Launcher.app"
zip -rq ../${GAME_TITLE}_OSX.zip .
cd ../..

#########
# LINUX #
#########
echo "Creating Linux Deployment"
rm -rf Deploy/Linux
rm -rf Deploy/${GAME_TITLE}_Linux.zip
mkdir -p Deploy/Linux

# Add header
cat > Deploy/Linux/${GAME_TITLE}Launcher.sh << END
#!/bin/sh
if [ ! -f "${GAME_TITLE}Launcher.exe" ]
then
    echo "Unpacking ${GAME_LONG_TITLE} Launcher..."
    tail -n +20 \$0 | uudecode
fi
if command -v mono > /dev/null
then
    if [ \`uname\`=="Darwin" ]
    then
        echo "Fixing Mono library paths..."
        export DYLD_FALLBACK_LIBRARY_PATH="/Library/Frameworks/Mono.framework/Versions/Current/lib:\${DYLD_FALLBACK_LIBRARY_PATH}:/usr/lib"
    fi
    echo "Starting ${GAME_LONG_TITLE} Launcher..."
    mono ${GAME_TITLE}Launcher.exe
else
    echo "${GAME_LONG_TITLE} requires Mono. Get it at http://www.mono-project.org"
fi
exit
END

# Add payload
uuencode -m Deploy/Common/${GAME_TITLE}.exe ${GAME_TITLE}Launcher.exe >> Deploy/Linux/${GAME_TITLE}Launcher.sh

# Zip
cd Deploy/Linux
zip -rq ../${GAME_TITLE}_Linux.zip .
cd ../..

