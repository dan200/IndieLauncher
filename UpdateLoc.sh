#!/bin/sh

mono \
	../LanguageExport/LanguageExport/bin/Release/UpdateLoc.exe \
	https://docs.google.com/spreadsheets/d/18FjL2zScJZNa1Eqgl3jAjQh_4YB6PZI0JzvbKV4DHUc/edit?usp=sharing \
	IndieLauncher/Resources/Languages \
	> /dev/null
