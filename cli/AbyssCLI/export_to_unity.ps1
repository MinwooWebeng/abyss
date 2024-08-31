Remove-Item D:\unity\AbyssUI\AbyssCLI\* -Recurse
Copy-Item -Path .\bin\Debug\net8.0\* -Destination D:\unity\AbyssUI\AbyssCLI -Recurse
Copy-Item -Path .\ABI\* -Destination D:\Unity\AbyssUI\Assets\Host\ABI -Recurse