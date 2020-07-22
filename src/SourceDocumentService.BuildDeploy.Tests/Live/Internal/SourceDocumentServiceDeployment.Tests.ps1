Describe "UKHO.SourceDocumentService Deployment Tests" {
    $WebAppPoolName = "UKHO.SourceDocumentService"
    $WebSiteName = "UKHO.EventsAndRules"
    $BindingHostname = "api.ukho.gov.uk"
    $BindingPort = 82
    $HttpsBindingPort = 443
    $PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"   
    $WebApplicationName = "SourceDocumentService" 
    $adUserDomain = "UKHO"    
    $adUser = "SVC_TMSdocSvcPoolPrd"
    $adGroup = "AG_PCPLiveIISSVC-GG"

    $adPermittedUsersGroup = "AG_TMLiveSDocSvcApi-DL"
    $adPermittedUsersGroupMembers = @("AG_PCPLiveIISSVC-GG")

    Context "Resource Planning Service API for Live" {
        It "IIS Web Server is running" {
            get-service -name "W3SVC" | select-object -ExpandProperty "Status" | Should be 'Running'
        }

        It "App Pool is running" {
            Get-WebAppPoolState -name "$WebAppPoolName"| select-object -ExpandProperty "Value" | Should be 'Started'
        }

        It "Web Site is running" {
            Get-Website -name "$WebSiteName"| select-object -ExpandProperty "State" | Should be 'Started'
        }

        It "is listening on port $BindingPort" {
            Test-NetConnection -ComputerName localhost -Port $BindingPort
        }
        
        It "Default Web Site is stopped" {
            Get-Website -name "Default Web Site"| select-object -ExpandProperty "State" | Should be 'Stopped'
        }

        It "Default Web Site is listening on port 9999" {
            (Get-WebBinding -name "Default Web Site" | select-object -ExpandProperty "bindingInformation") `
            -like "*9999*" `
            | Should be $true
        }

        It "The load balanced binding is set up on $BindingHostname" {
            Test-NetConnection -ComputerName $BindingHostname -Port $BindingPort
        }

        It "Web Application $WebApplicationName belongs to web site $WebSiteName" {
            (Get-WebApplication -site "$WebSiteName" -Name "$WebApplicationName").Count | Should be '1'
        }

        It "Web Site has binding $BindingHostname with port $BindingPort" {
            (Get-WebBinding -Name "$WebSiteName" `
                    | Where-Object bindingInformation -Match "$($BindingPort):$($BindingHostname)").Count `
                    | Should be '1'
        }

        It "Web Site has binding $BindingHostname with port $HttpsBindingPort" {
            (Get-WebBinding -Name "$WebSiteName" `
                    | Where-Object bindingInformation -Match "$($HttpsBindingPort):$($BindingHostname)").Count `
                    | Should be '1'
        }

        It "is has a App Pool Account $adUser" {
            Get-ADUser -Identity $adUser | select-object -ExpandProperty "Enabled" | Should be $true
        }
        It "is has a App Pool Account $adUser is part of $adGroup group" {
            $array = Get-ADPrincipalGroupMembership $adUser | select-object -ExpandProperty "name"
            $array -icontains $adGroup | should be $true
        }
        It "is App Pool using account $adUser" {
            Get-Item (Join-Path 'IIS:\AppPools\' $WebAppPoolName) | select -ExpandProperty processModel | select -expand userName | should be $adUser
        }
        It "is ad user $adUser member of IIS_IUSRS" {
            (net localgroup IIS_IUSRS | Select-String "$adUserDomain\$adUser" -SimpleMatch) -ne $null | Should be $true
        }
        It "is IIS_IUSRS has Modify control over '$PhysicalPathWebSite'" {
            ( Get-ACL -Path "$PhysicalPathWebSite" `
                | Select-Object -ExpandProperty Access `
                | Where-Object identityreference -eq "BUILTIN\IIS_IUSRS" `
                | Where-Object FileSystemRights -Match "Modify" `
                ) -ne $null | Should be $true
        }
        It "has an AD group called $adPermittedUsersGroup"{
            Get-ADGroup -Identity $adPermittedUsersGroup 
        }
        It "has permitted user groups as a members of $adPermittedUsersGroup"{
            $adPermittedUsersGroupMembers | ForEach-Object {
                if($null -eq (Get-ADGroupMember $adPermittedUsersGroup | Where-Object -Property name -EQ -Value $_)) {
                    throw "Group member $_ not found in $adPermittedUsersGroup" }
            }
        }
    }
}
