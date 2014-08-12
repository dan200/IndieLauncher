#!/bin/sh

# Settings
GAME_TITLE=ExampleGame
GAME_LONG_TITLE="Example Game"

# Common
rm -rf Deploy
mkdir Deploy
mkdir Deploy/Common
cp IndieLauncher/bin/Release/IndieLauncher.exe Deploy/Common/${GAME_TITLE}.exe

###########
# WINDOWS #
###########
echo "Creating Windows Deployment"
rm -rf Deploy/Windows
rm -rf Deploy/${GAME_TITLE}_Windows.zip
mkdir -p Deploy/Windows
cd Deploy/Windows
cp ../Common/${GAME_TITLE}.exe .
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
macpack -n "${GAME_LONG_TITLE}" -i ../../Icon.icns -m cocoa ../Common/${GAME_TITLE}.exe
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
cat > Deploy/Linux/${GAME_TITLE}.sh << END
#!/bin/sh
if [ ! -f "${GAME_TITLE}.exe" ]
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
    mono ${GAME_TITLE}.exe
else
    echo "${GAME_LONG_TITLE} requires Mono. Get it at http://www.mono-project.org"
fi
exit
END

# Add payload
uuencode -m Deploy/Common/${GAME_TITLE}.exe ${GAME_TITLE}.exe >> Deploy/Linux/${GAME_TITLE}.sh

# Zip
cd Deploy/Linux
zip -rq ../${GAME_TITLE}_Linux.zip .
cd ../..

