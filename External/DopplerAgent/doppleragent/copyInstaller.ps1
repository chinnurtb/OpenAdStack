param( 
    [string]
    [Parameter(Mandatory=$true)]
    $DestinationPath
)

$RootDir = split-path (split-path $MyInvocation.MyCommand.Path)

xcopy /frys "$RootDir\Doppler.Installer\Doppler.Installer.exe" "$DestinationPath\"
xcopy /frys "$RootDir\Doppler.Installer\config.txt" "$DestinationPath\"

"*.bat", "*.ps1", "*.exe", "*.exe.config", "*.dll" | % {
    xcopy /frys "$RootDir\Doppler.Installer\$_" "$DestinationPath\doppleragent\"
    xcopy /frys "$RootDir\ScomAdapterService\$_" "$DestinationPath\doppleragent\"
    xcopy /frys "$RootDir\GacUtility\$_" "$DestinationPath\doppleragent\"
    xcopy /frys "$RootDir\DataStorage\$_" "$DestinationPath\doppleragent\"
    xcopy /frys "$RootDir\TraceListeners\$_" "$DestinationPath\doppleragent\"
}

xcopy /frys "$RootDir\TraceListeners\Doppler.TraceListeners.XML" "$DestinationPath\doppleragent\"
