go build -o abyssnet.dll -buildmode=c-shared .
Copy-Item "./abyssnet.dll" -Destination "../cli/AbyssCLI/bin/Release/net8.0"