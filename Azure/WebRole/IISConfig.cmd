REM %windir%\system32\inetsrv\appcmd set config  -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='application/json; charset=utf-8',enabled='True']" /commit:apphost

REM Remove old settings - keeps local deploys working (since you get errors otherwise)
%windir%\system32\inetsrv\appcmd reset config -section:urlCompression
%windir%\system32\inetsrv\appcmd reset config -section:system.webServer/httpCompression 

REM urlCompression - is this needed?
%windir%\system32\inetsrv\appcmd set config -section:urlCompression /doDynamicCompression:True /commit:apphost
REM Enable json mime type
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='application/json; charset=utf-8',enabled='True']" /commit:apphost

REM IIS Defaults
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='text/*',enabled='True']" /commit:apphost
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='message/*',enabled='True']" /commit:apphost
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='application/x-javascript',enabled='True']" /commit:apphost
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='*/*',enabled='False']" /commit:apphost
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"staticTypes.[mimeType='text/*',enabled='True']" /commit:apphost
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"staticTypes.[mimeType='message/*',enabled='True']" /commit:apphost
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"staticTypes.[mimeType='application/javascript',enabled='True']" /commit:apphost
%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"staticTypes.[mimeType='*/*',enabled='False']" /commit:apphost

REM Set dynamic compression level to appropriate level.  Note gzip will already be present because of reset above, but compression level will be zero after reset.
%windir%\system32\inetsrv\appcmd.exe set config -section:system.webServer/httpCompression /+"[name='deflate',doStaticCompression='True',doDynamicCompression='True',dynamicCompressionLevel='7',dll='%%Windir%%\system32\inetsrv\gzip.dll']" /commit:apphost
%windir%\system32\inetsrv\appcmd.exe set config -section:system.webServer/httpCompression -[name='gzip'].dynamicCompressionLevel:7 /commit:apphost

EXIT /b 0