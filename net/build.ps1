go build -o abyssnet.dll -buildmode=c-shared .
Copy-Item "./abyssnet.dll" -Destination "../cli/AbyssCLI/bin/Release/net8.0"
Copy-Item "./abyssnet.dll" -Destination "../cli/AbyssCLI/bin/Debug/net8.0"