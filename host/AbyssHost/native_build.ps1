dotnet publish . -r win-x64 --property:OutputPath=./win-x64-native
Copy-Item -Path .\win-x64-nativepublish\* -Destination D:\unity\AbyssUI\AbyssHost -Recurse