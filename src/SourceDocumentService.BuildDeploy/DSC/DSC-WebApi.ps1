Node $AllNodes.Where{$_.Roles -eq "WebAPI"}.NodeName
{
    # Install the IIS role
    WindowsFeature IIS
    {
        Ensure          = 'Present'
        Name            = 'Web-Server'
    }

    # Install the ASP .NET 4.5 role
    WindowsFeature AspNet45
    {
        Ensure          = 'Present'
        Name            = 'Web-Asp-Net45'
    }

    # Ensure Windows Authentication is added
    WindowsFeature Web-Windows-Auth_Feature
    {
        Name = "Web-Windows-Auth"
        Ensure = "Present"
    }

    WindowsFeature BasicAuthentication
    {
        Name = "Web-Basic-Auth"
        Ensure = "Present" 
    }

    # Disable loopback check, as we're using FQDN in our IIS binding
    Registry BackConnectionRegistry
    {
        Ensure      = "Present"
        Key         = "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0"
        ValueName   = "BackConnectionHostNames"
        ValueData   = $node.BackConnectionHostNames
    }

    # Create a Web Application Pool
    xWebAppPool WebAppPool
    {
        Name   = $Node.WebAppPoolName
        Ensure = "Present"
        State  = "Started"
        startMode = "AlwaysRunning"
        autoStart = $true
        LoadUserProfile = $false
        identityType = "SpecificUser"
        Credential = $Node.WebAppPoolCredentials
    }

    # Create physical path website
    File WebsitePath
    {
        DestinationPath = $Node.PhysicalPathWebApplication
        Type = "Directory"
        Ensure = "Present"
    }

    # Ensure IIS_IUSRS have full control over C:\inetpub\UKHO.Services
    cNtfsPermissionEntry PermissionSet_WebsitePath
    {
        Ensure = 'Present'
        Path = $Node.PhysicalPathWebSite
        Principal = 'BUILTIN\IIS_IUSRS'
        AccessControlInformation = @(
            cNtfsAccessControlInformation
            {
                AccessControlType = 'Allow'
                FileSystemRights = 'Modify'
                Inheritance = 'ThisFolderSubfoldersAndFiles'
                NoPropagateInherit = $false
            }
        )
        DependsOn = '[File]WebsitePath'
    }

    xIisFeatureDelegation anonymousAuthentication
    {
        SectionName      = 'security/authentication/anonymousAuthentication'
        OverrideMode   = 'Allow'
    }
    xIisFeatureDelegation basicAuthentication
    {
        SectionName      = 'security/authentication/basicAuthentication'
        OverrideMode   = 'Allow'
    }
    xIisFeatureDelegation windowsAuthentication
    {
        SectionName      = 'security/authentication/windowsAuthentication'
        OverrideMode   = 'Allow'
    }

    # Ensure "Default Web Site" is stopped
    xWebSite DefaultWebsite
    {
        Name = "Default Web Site"
        State = "Stopped"
        BindingInfo = MSFT_xWebBindingInformation
        {
            Protocol = "http"
            Port = "9999"
            HostName = ""
        }
    }

    # Create a New Website with Port
    xWebSite WebSite
    {
        Name   = $Node.WebSiteName
        Ensure = "Present"
        BindingInfo = @(
                        @(MSFT_xWebBindingInformation
                            {
                                Protocol = "http"
                                Port = $Node.BindingPort
                                HostName = $Node.BindingHostname
                            }
                        );
                        @(MSFT_xWebBindingInformation
                            {
                                Protocol = "https"
                                Port = $Node.HttpsBindingPort
                                HostName = $Node.BindingHostname
                                CertificateStoreName = "MY"
                                CertificateThumbprint = $Node.CertThumbprint
                                IPAddress = "*"
                                SslFlags = 0
                            }
                        )
                    )
        ApplicationPool = "UKHO.EventsAndRules"
        PhysicalPath = $Node.PhysicalPathWebSite
        State = "Started"
        DependsOn = @("[xWebAppPool]WebAppPool", `
                        "[File]WebsitePath", `
                        "[cNtfsPermissionEntry]PermissionSet_WebsitePath", `
                        "[xIisFeatureDelegation]basicAuthentication", `
                        "[xIisFeatureDelegation]windowsAuthentication", `
                        "[xIisFeatureDelegation]anonymousAuthentication")

        AuthenticationInfo = MSFT_xWebAuthenticationInformation
        {
            Anonymous = $False
            Basic = $True
            Windows = $True
        }
    }

    # Create a new Web Application
    xWebApplication WebApplication
    {
        Name = $Node.WebApplicationName
        Website = $Node.WebSiteName
        WebAppPool =  $Node.WebAppPoolName
        PhysicalPath = $Node.PhysicalPathWebApplication
        Ensure = "Present"
        DependsOn = @("[xWebSite]WebSite")
    }

    # DSC resource only stops app pool, copies in files then restarts (and does config transforms)
    $appSourcePath = $ConfigurationData.NonNodeData.DeploymentArtifacts.Where{ $_.Roles -eq "WebApi" }.TargetDirectory
    UKHO_WebsiteDsc "WebApi" {
        AppPoolName = $Node.WebAppPoolName
        SourcePath = $appSourcePath | select-object -First 1
        Ensure = "Present"
        DestinationPath = $Node.PhysicalPathWebApplication
        FoldersToPreserve = $ConfigurationData.NonNodeData.FoldersToPreserve
        WebsiteName = $Node.WebSiteName
        TransformFileFilter = "*.config"
        TransformDataObject = $Node.WebConfigTransformDataObject      
    }
}
