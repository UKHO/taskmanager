if ($null -ne $ConfigData.NonNodeData.ServiceCred) {
    $contentServicePassword = $ConfigData.NonNodeData.ServiceCred.GetNetworkCredential().Password
}

$ConfigData.AllNodes = @(
    @{
        NodeName = "HPCPPREWEB01.Engineering.UKHO.gov.uk"
        PSDscAllowDomainUser = $true
        Roles = [string[]]@("WebAPI")
        PsDscAllowPlainTextPassword = $true
        WebAppPoolName = "UKHO.SourceDocumentService"
        WebSiteName = "UKHO.EventsAndRules"
        BindingHostname = "pre.api.engineering.ukho.gov.uk"
        BindingPort = 82
        HttpsBindingPort = 443
        CertThumbprint = "1bbcfd0c8198c35f2230b3719210f909110d202d"
        PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"
        WebApplicationName = "SourceDocumentService"
        PhysicalPathWebApplication = "C:\inetpub\UKHO.EventsAndRules\SourceDocumentService"
        WebAppPoolCredentials = $ConfigData.NonNodeData.ServiceCred
        BackConnectionHostNames = "pre.api.engineering.ukho.gov.uk"
        MaxEnvelopeSize = 8192
        SetServerCredSSP = $True
        CertificateFile = "HPCPPREWEB01.Engineering.UKHO.gov.uk-DscEncryptionCert.cer"         
        Thumbprint = ""
        WebConfigTransformDataObject = @{
            PermittedRoles = "AG_TMPreSDocSvcApi-DL"
            FilestorePath = "\\engineering.ukho.gov.uk\\dfs\\AppData\\HDB\\PRE\\SDRADocuments\\"
            ContentServiceBaseUrl = "http://at2.csp.api.engineering.ukho.gov.uk/Content/PCP-PRE-LIVE/V1/"
            ContentServiceUsername = $ConfigData.NonNodeData.ServiceCred.UserName
            ContentServicePassword = $contentServicePassword
        }
    }
    @{
        NodeName = "HPCPPREWEB02.Engineering.UKHO.gov.uk"
        PSDscAllowDomainUser = $true
        Roles = [string[]]@("WebAPI")
        PsDscAllowPlainTextPassword = $true
        WebAppPoolName = "UKHO.SourceDocumentService"
        WebSiteName = "UKHO.EventsAndRules"
        BindingHostname = "pre.api.engineering.ukho.gov.uk"
        BindingPort = 82
        HttpsBindingPort = 443
        CertThumbprint = "061fec6308b50d360646b6b75137a496511a58ba"
        PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"
        WebApplicationName = "SourceDocumentService"
        PhysicalPathWebApplication = "C:\inetpub\UKHO.EventsAndRules\SourceDocumentService"
        WebAppPoolCredentials = $ConfigData.NonNodeData.ServiceCred
        BackConnectionHostNames = "pre.api.engineering.ukho.gov.uk"
        MaxEnvelopeSize = 8192
        SetServerCredSSP = $True
        CertificateFile = "HPCPPREWEB02.Engineering.UKHO.gov.uk-DscEncryptionCert.cer"         
        Thumbprint = ""
        WebConfigTransformDataObject = @{
            PermittedRoles = "AG_TMPreSDocSvcApi-DL"
            FilestorePath = "\\engineering.ukho.gov.uk\\dfs\\AppData\\HDB\\PRE\\SDRADocuments\\"
            ContentServiceBaseUrl = "http://at2.csp.api.engineering.ukho.gov.uk/Content/PCP-PRE-LIVE/V1/"
            ContentServiceUsername = $ConfigData.NonNodeData.ServiceCred.UserName
            ContentServicePassword = $contentServicePassword
        }
    }
)

$ConfigData.NonNodeData.ServiceAccountSetup = @{
    Domain = "engineering.ukho.gov.uk"
    ServiceAccountsOU = "OU=Service Accounts,OU=PRE,OU=TM,OU=Services,DC=Engineering,DC=ukho,DC=gov,DC=uk"
    AccountGroups ="AG_PCPPREIISSVC-GG"
    AddToIISUserGroup = $true
}
