$ErrorActionPreference = "Stop"
dotnet publish --configuration Release
$publishDir = "$PSScriptRoot\bin\Release\net5.0\publish\"
$remoteSession = New-Ec2Session.ps1
Invoke-Command -Session $remoteSession { Stop-IISSite -Name "name_generator" }
Copy-Item -Recurse -Force -Path "$publishDir\*" -ToSession $remoteSession -Destination "C:\Users\admin\Desktop\Sitepath"
Invoke-Command -Session $remoteSession { Start-IISSite -Name "name_generator" }
Remove-PSSession $remoteSession