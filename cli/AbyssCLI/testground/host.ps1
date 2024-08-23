..\bin\Release\net8.0\AbyssCLI.exe $args
Write-Output "Press any key to continue ..."
$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null