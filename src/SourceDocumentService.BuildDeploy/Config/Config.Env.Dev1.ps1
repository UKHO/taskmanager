if ($null -ne $ConfigData.NonNodeData.ServiceCred) {
    $contentServicePassword = $ConfigData.NonNodeData.ServiceCred.GetNetworkCredential().Password
}

$ConfigData.AllNodes = @(
    @{
        NodeName = "HPCPDEV1WEB01.Engineering.UKHO.gov.uk"
        PSDscAllowDomainUser = $true
        Roles = [string[]]@("WebAPI")
        PsDscAllowPlainTextPassword = $true
        WebAppPoolName = "UKHO.SourceDocumentService"
        WebSiteName = "UKHO.EventsAndRules"
        BindingHostname = "dev1.api.engineering.ukho.gov.uk"
        BindingPort = 82
        HttpsBindingPort = 443
        CertThumbprint = "2d61da296529a9ff5ab5a3e6677f0148a3c1b3ea"
        PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"
        WebApplicationName = "SourceDocumentService"
        PhysicalPathWebApplication = "C:\inetpub\UKHO.EventsAndRules\SourceDocumentService"
        WebAppPoolCredentials = $ConfigData.NonNodeData.ServiceCred
        BackConnectionHostNames = "dev1.api.engineering.ukho.gov.uk"
        MaxEnvelopeSize = 8192
        SetServerCredSSP = $True
        CertificateFile = "HPCPDEV1WEB01.Engineering.UKHO.gov.uk-DscEncryptionCert.cer"         
        Thumbprint = ""
        WebConfigTransformDataObject = @{
            PermittedRoles = "AG_TMDEV1SDocSvcApi-DL"
            FilestorePath = "\\engineering.ukho.gov.uk\\dfs\\AppData\\HDB\\DEV1\\SDRADocuments\\"
            ContentServiceBaseUrl = "http://at2.csp.api.engineering.ukho.gov.uk/Content/PCP-DEV1/V1/"
            ContentServiceUsername = $ConfigData.NonNodeData.ServiceCred.UserName
            ContentServicePassword = $contentServicePassword
            CuiaDatabaseConnectionString = "Data Source=OWPDev1_List.engineering.ukho.gov.uk;initial catalog=Cuiaworkspace-DEV1;Integrated Security=True;MultiSubnetFailover=True;MultipleActiveResultSets=True"
            CuiaDatabaseTimeoutInSeconds = 30
        }
    }
    @{
        NodeName = "HPCPDEV1WEB02.Engineering.UKHO.gov.uk"
        PSDscAllowDomainUser = $true
        Roles = [string[]]@("WebAPI")
        PsDscAllowPlainTextPassword = $true
        WebAppPoolName = "UKHO.SourceDocumentService"
        WebSiteName = "UKHO.EventsAndRules"
        BindingHostname = "dev1.api.engineering.ukho.gov.uk"
        BindingPort = 82
        HttpsBindingPort = 443
        CertThumbprint = "dccc9796b8509a0f5692adffdeb9cdf6b86c9d3a"
        PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"
        WebApplicationName = "SourceDocumentService"
        PhysicalPathWebApplication = "C:\inetpub\UKHO.EventsAndRules\SourceDocumentService"
        WebAppPoolCredentials = $ConfigData.NonNodeData.ServiceCred
        BackConnectionHostNames = "dev1.api.engineering.ukho.gov.uk"
        MaxEnvelopeSize = 8192
        SetServerCredSSP = $True
        CertificateFile = "HPCPDEV1WEB02.Engineering.UKHO.gov.uk-DscEncryptionCert.cer"
        Thumbprint = ""
        WebConfigTransformDataObject = @{
            PermittedRoles = "AG_TMDEV1SDocSvcApi-DL"
            FilestorePath = "\\engineering.ukho.gov.uk\\dfs\\AppData\\HDB\\DEV1\\SDRADocuments\\"
            ContentServiceBaseUrl = "http://at2.csp.api.engineering.ukho.gov.uk/Content/PCP-DEV1/V1/"
            ContentServiceUsername = $ConfigData.NonNodeData.ServiceCred.UserName
            ContentServicePassword = $contentServicePassword
            CuiaDatabaseConnectionString = "Data Source=OWPDev1_List.engineering.ukho.gov.uk;initial catalog=Cuiaworkspace-DEV1;Integrated Security=True;MultiSubnetFailover=True;MultipleActiveResultSets=True"
            CuiaDatabaseTimeoutInSeconds = 30
        }
    }
)

$ConfigData.NonNodeData.ServiceAccountSetup = @{
    Domain = "engineering.ukho.gov.uk"
    ServiceAccountsOU = "OU=Service Accounts,OU=DEV1,OU=TM,OU=Services,DC=Engineering,DC=ukho,DC=gov,DC=uk"
    AccountGroups ="AG_PCPDEV1IISSVC-GG"
    AddToIISUserGroup = $true
}
