function Log-Message
{
    param(
        [string] $message
    )
    
    Write-Output ("{0}: {1}" -f (Get-Date), $message)
}

function Get-ConfigPaths
{                 
    $configPaths = "approot", "sitesroot" | ForEach {
    
        $folderToSearch = Join-Path $env:roleroot $_
        if (Test-Path (Join-Path $env:roleroot $_))
        {
            dir $folderToSearch -Recurse -Filter web.config
            dir $folderToSearch -Recurse -Filter *.dll.config
        }
    }

    return ( $configPaths | ForEach { $_.FullName } )		
}

function Create-ListenerNode
{
    param (
        $configFile
    )
    
    $listener = $configFile.CreateElement("add")
    $listener.SetAttribute("name", "DopplerAgentTraceListener")
    $listener.SetAttribute("type", "Doppler.TraceListeners.DopplerAgentTraceListener, Doppler.TraceListeners, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e8e00515cbf8fa76")
    
    return $listener
}

function Create-ModuleNode
{
    param (
        $configFile
    )

    $httpModule = $configFile.CreateElement("add")
    $httpModule.SetAttribute("name", "DopplerInterceptionHttpModule")
    $httpModule.SetAttribute("type", "Doppler.TraceListeners.DopplerInterceptionHttpModule, Doppler.TraceListeners, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e8e00515cbf8fa76")
    
    return $httpModule
}

function Does-DopplerListenerExist
{
    param(
        $configFile
    )
    
    $listener = $configFile.SelectSingleNode("configuration/system.diagnostics/trace/listeners/add[@name='DopplerAgentTraceListener']")
    return ( $listener -ne $null )
}

function Does-DopplerHttpModuleExist
{
    param(
        $configFile
    )
    
    $httpModule = $configFile.SelectSingleNode("configuration/system.webServer/modules/add[@name='DopplerInterceptionHttpModule']")
    return ( $httpModule -ne $null )
}

function Append-NodeToXPath
{
    param(
        $configFile,
        [string] $xPath,
        [System.Xml.XmlNode] $newChildNode
    )
    
    $splitXPaths = $xPath.Trim("/").Split("/")
    $xPathSplitIndex = $splitXPaths.Count
     
    for ($xPathSplitIndex = $splitXPaths.Count; $xPathSplitIndex -gt 0; $xPathSplitIndex--)    
    { 
        $subXpath = [String]::Join("/", $splitXPaths, 0, $xPathSplitIndex); 
        $node = $configFile.SelectSingleNode($subXpath); 
        
        # Found first existant node from bottom
        if ($node -ne $null) 
        { 
            # Append descendants in xPath
            for ($descXPathIndex = $xPathSplitIndex; $descXPathIndex -lt $splitXPaths.Count; $descXPathIndex++) 
            { 
                $descendant = $configFile.CreateElement($splitXPaths[$descXPathIndex])
                $node = $node.AppendChild($descendant); 
            } 
            
            $node.AppendChild($newChildNode) | Out-Null
            break
        }        
    }    
}

function Add-DopplerListenerToConfig
{
    param (
    )
    
    Get-ConfigPaths | ForEach {    
        [xml]$configFile = gc $_
        
        Log-Message "Adding Doppler Listener to $_"
        if( Does-DopplerListenerExist $configFile ) 
        { 
            Log-Message "Doppler Listener already exists in $_"
            return 
        }        
        
        $listener = Create-ListenerNode $configFile
        Append-NodeToXPath $configFile "configuration/system.diagnostics/trace/listeners" $listener
        $configFile.Save($_)    
    }
}

function Add-DopplerHttpModuleToConfig
{
    param (
    )

    Get-ConfigPaths | Where { $_ -like '*Web.config' } | ForEach { 
        [xml]$configFile = gc $_            
    
        Log-Message "Adding Doppler HttpModule to $_"        
        if( Does-DopplerHttpModuleExist $configFile ) 
        { 
            Log-Message "Doppler HttpModule already exists in $_" 
            return 
        }        
        
        $httpModule = Create-ModuleNode $configFile
        $preHttpModuleXPath = "configuration/system.webServer/modules"       
        Append-NodeToXPath $configFile $preHttpModuleXPath $httpModule
                
        $configFile.SelectSingleNode($preHttpModuleXPath).SetAttribute("runAllManagedModulesForAllRequests", "true")       
        $configFile.Save($_)
    }
}

function Remove-DopplerListenerFromConfig
{
    param (
    )
    
    Get-ConfigPaths | ForEach {    
        [xml]$configFile = gc $_
        
        Log-Message "Removing Doppler Listener from $_"
        if(-NOT (Does-DopplerListenerExist $configFile)) 
        { 
            Log-Message "Doppler Listener doesn't exist in $_"
            return 
        }
                
        $listener = $configFile.configuration.'system.diagnostics'.trace.listeners.SelectSingleNode("add[@name='DopplerAgentTraceListener']")
        $configFile.configuration.'system.diagnostics'.trace.listeners.RemoveChild($listener) | Out-Null
        $configFile.Save($_)    
    }
}

function Remove-DopplerHttpModuleFromConfig
{
    param (
    )

    Get-ConfigPaths | Where { $_ -like '*Web.config' } | ForEach { 
        [xml]$configFile = gc $_           
        
        Log-Message "Removing Doppler HttpModule from $_"
        if(-NOT (Does-DopplerHttpModuleExist $configFile)) 
        { 
            Log-Message "Doppler HttpModule doesn't exist in $_"
            return 
        }        
        
        $httpModule = $configFile.configuration.'system.webServer'.modules.SelectSingleNode("add[@name='DopplerInterceptionHttpModule']")
        $configFile.configuration.'system.webServer'.modules.RemoveChild($httpModule) | Out-Null                
        $configFile.Save($_)    
    }
}
