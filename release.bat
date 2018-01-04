set MotronicSuite.version=1.2.4.0
devenv Motronic.sln /Rebuild Release /project SetupSuite

mkdir C:\users\mattias\Delivery\Motronic\%MotronicSuite.version%
xcopy SetupSuite\Release\MotronicSuite.msi C:\users\mattias\Delivery\Motronic\%MotronicSuite.version%\

echo ----------------------------------------------------
git changes
echo ----------------------------------------------------

git tag MotronicSuite_v%MotronicSuite.version%