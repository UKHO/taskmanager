param(
    [string]$Environment,
    [string]$ArtifactRootDirectory,
    [PSCredential]$DeployCred,
    [PSCredential]$ServiceCred
)

$ConfigData = $null

$ConfigData = 
@{
    NonNodeData =
    @{
        Environment = $Environment
        ArtifactRootDirectory = $ArtifactRootDirectory
        DeployCred = $DeployCred
        ServiceCred = $ServiceCred
        PoSHRepositories = @(
            @{
                Name = "UKHO.PSGallery"
                Type = "Proget"
                SourceUri = "http://proget.ukho.gov.uk/nuget/ukho.psgallery/"
                InstallationPolicy = "Trusted"
            }
        );
        PoSHPackages = @(
            @{
                Name="UKHO.WebsiteDSC"
                RequiredVersion = "4.0.0"
                Repository = "UKHO.PSGallery"
                DSCResource = $true
                Usages = @("Release", "Node")
            },
            @{
                Name="xWebAdministration"
                RequiredVersion = "1.19.0.0"
                Repository = "UKHO.PSGallery"
                DSCResource = $true
                Usages = @("Build", "Node")
            },
            @{
                Name="cNtfsAccessControl"
                RequiredVersion = "1.3.1"
                Repository = "UKHO.PSGallery"
                DSCResource = $true
                Usages = @("Build", "Node")
            },
            @{
                Name="UKHO.PSModuleInstaller"
                MinimumVersion = "1.1.0"
                MaximumVersion = "1.9999.99999"
                Repository = "UKHO.PSGallery"
                # Get installed separately
                Usages = @("")
            },
            @{
                Name="UKHO.Operations.BuildDeployTools"
                MinimumVersion = "3.7.8"
                MaximumVersion = "3.9999.99999"
                Repository = "UKHO.PSGallery"
                Usages = @("Build", "Release")
            },
            @{
                Name="UKHO.BuildAndDeploy"
                MinimumVersion = "6.0.0"
                MaximumVersion = "6.9999.99999"
                Repository = "UKHO.PSGallery"
                Usages = @("Release", "Node")
            }
        );
        DeploymentArtifacts = @(
            @{
                Name = "CompiledDSCMOFFiles"
                CleanOnDeploy = $true
            },
            @{
                Name = "SourceDocumentService"
                Roles = @("WebApi")
                CopyToNodes = $true
                TargetDirectory = "c:\temp\SourceDocumentServiceDeploy\SourceDocumentService"
                CleanOnDeploy = $true
            },
            @{
                Name = "UKHO.SourceDocumentService.BuildDeploy"
                Roles = @("WebApi", "BuildDeploy")
                CopyToNodes = $true
                TargetDirectory = "c:\temp\SourceDocumentServiceDeploy\SourceDocumentService.BuildDeploy"
                CleanOnDeploy = $true
            }
        )
    } 
}

# Adding the current dir for each artifact i.e. where it is on release server
$ConfigData.NonNodeData.DeploymentArtifacts | ForEach-Object {
    $_.ArtifactLocation = "{0}\{1}" -f $ArtifactRootDirectory, $_.Name
}

# Check env has specific config - load, else throw error
if([string]::IsNullOrEmpty($Environment) -eq $false) {
    if(Test-Path -Path "$PSScriptRoot\Config.Env.$Environment.ps1") {
        . $PSScriptRoot\Config.Env.$Environment.ps1
    } else {
        Write-Warning "There is no configuration file for the environment $Environment. Only the base configuration has been initialised."
    }

} else {
    Write-Warning "The environment was not specified. Only the base configuration has been loaded. This may cause problems deploying."
}

. $PSScriptRoot\Config.Validation.Tests.ps1

Validate-Configuration -ConfigData $ConfigData

