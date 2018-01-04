set MotronicSuite.version=1.2.4.0
devenv Motronic.sln /Rebuild Release /project SetupSuite

pushd SetupSuite\Release\
"C:\md5sum.exe" \MotronicSuite.msi >> \MotronicSuite.md5
popd

mkdir z:\Motronic\%MotronicSuite.version%
xcopy SetupSuite\Release\MotronicSuite.msi z:\Motronic\%MotronicSuite.version%\

echo ^<?xml version="1.0" encoding="utf-8"?^>  > z:\Motronic\version.xml
echo ^<motronic version="%MotronicSuite.version%"/^> >> z:\Motronic\version.xml

echo ----------------------------------------------------
git changes
echo ----------------------------------------------------

git tag MotronicSuite_v%MotronicSuite.version%