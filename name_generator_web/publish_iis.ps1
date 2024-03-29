$ErrorActionPreference = "Stop"
dotnet publish --configuration Release
Compress-Archive -Path "bin\Release\net5.0\publish\*" -DestinationPath "name_generator.zip" -Update
$remoteSession = New-Ec2Session.ps1
Try {
    Copy-Item -Recurse -Force -Path "name_generator.zip" -ToSession $remoteSession -Destination "C:\Users\Administrator\Desktop\name_generator.zip"
    Invoke-Command -Session $remoteSession { 
        iisreset.exe /stop
        Remove-Item -Recurse -Force "C:\Users\Administrator\Desktop\Sitepath\*"
        Expand-Archive -Force -LiteralPath "C:\Users\Administrator\Desktop\name_generator.zip" -DestinationPath "C:\Users\Administrator\Desktop\Sitepath"
        iisreset.exe /start 
    }
} Finally {
    Remove-PSSession $remoteSession
}
