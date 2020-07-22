if ($null -ne $ConfigData.NonNodeData.ServiceCred) {
    $contentServicePassword = $ConfigData.NonNodeData.ServiceCred.GetNetworkCredential().Password
}

$ConfigData.AllNodes = @(
    @{
        NodeName = "HPCPAT1WEB01.Engineering.UKHO.gov.uk"
        PSDscAllowDomainUser = $true
        Roles = [string[]]@("WebAPI")
        PsDscAllowPlainTextPassword = $true
        WebAppPoolName = "UKHO.SourceDocumentService"
        WebSiteName = "UKHO.EventsAndRules"
        BindingHostname = "at1.api.engineering.ukho.gov.uk"
        BindingPort = 82
        HttpsBindingPort = 443
        CertThumbprint = "69724adcea62ca47d5a38f1e9296f9d2323ceff5"
        PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"
        WebApplicationName = "SourceDocumentService"
        PhysicalPathWebApplication = "C:\inetpub\UKHO.EventsAndRules\SourceDocumentService"
        WebAppPoolCredentials = $ConfigData.NonNodeData.ServiceCred
        BackConnectionHostNames = "at1.api.engineering.ukho.gov.uk"
        MaxEnvelopeSize = 8192
        SetServerCredSSP = $True
        CertificateFile = "HPCPAT1WEB01.Engineering.UKHO.gov.uk-DscEncryptionCert.cer"         
        Thumbprint = ""
        WebConfigTransformDataObject = @{
            PermittedRoles = "AG_TMAT1SDocSvcApi-DL"
            FilestorePath = "\\engineering.ukho.gov.uk\\dfs\\AppData\\HDB\\AT1\\SDRADocuments"
            ContentServiceBaseUrl = "http://at2.csp.api.engineering.ukho.gov.uk/Content/PCP-AT1/V1/"
            ContentServiceUsername = $ConfigData.NonNodeData.ServiceCred.UserName
            ContentServicePassword = $contentServicePassword
        }
    }
)

$ConfigData.NonNodeData.ServiceAccountSetup = @{
    Domain = "engineering.ukho.gov.uk"
    ServiceAccountsOU = "OU=Service Accounts,OU=AT1,OU=TM,OU=Services,DC=Engineering,DC=ukho,DC=gov,DC=uk"
    AccountGroups ="AG_PCPAT1IISSVC-GG"
    AddToIISUserGroup = $true
}
