set MotronicSuite.version=1.2.3.0
devenv Motronic.sln /Rebuild Release /project SetupSuite
mkdir C:\users\mattias\Dropbox\public\Motronic\%MotronicSuite.version%
xcopy SetupSuite\Release\MotronicSuite.msi C:\users\mattias\Dropbox\public\Motronic\%MotronicSuite.version%\

git tag MotronicSuite_v%MotronicSuite.version%