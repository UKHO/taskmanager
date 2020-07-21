
Configuration DSC-Config {

    #Currently, there appears to be a limitation whereby the import dscresource has to be a static value. 
    #The ideal is that this eventually pulls from the config ($ConfigurationData.NonNodeData.Packages) - CM
    Import-DscResource -ModuleName PSDesiredStateConfiguration
    Import-DscResource -ModuleName xWebAdministration -ModuleVersion "1.19.0.0"
    Import-DscResource -ModuleName cNtfsAccessControl -ModuleVersion "1.3.1"
    Import-DscResource -ModuleName UKHO.WebsiteDSC -ModuleVersion "4.0.0"
    
    Node $AllNodes.NodeName {

        LocalConfigurationManager
        {
            RebootNodeIfNeeded = $true
            ActionAfterReboot = 'ContinueConfiguration'
            ConfigurationMode = 'ApplyOnly'
            RefreshMode = 'Push'
            CertificateId = $node.Thumbprint
        }

        WindowsFeature Net45 {
            Name = "NET-Framework-45-Core"
            Ensure = "Present"
        }       
        
        WindowsFeature ActiveDirectoryLightweightDirectoryServices {
            Name = "ADLDS"
            Ensure = "Present"
        }         
    }
    . $PSScriptRoot\DSC-WebApi.ps1    
}
