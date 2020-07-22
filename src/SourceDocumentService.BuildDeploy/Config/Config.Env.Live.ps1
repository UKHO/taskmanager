if ($null -ne $ConfigData.NonNodeData.ServiceCred) {
    $contentServicePassword = $ConfigData.NonNodeData.ServiceCred.GetNetworkCredential().Password
}

$ConfigData.AllNodes = @(
    @{
        NodeName = "HPCPPRDWEB01.business.UKHO.gov.uk"
        PSDscAllowDomainUser = $true
        Roles = [string[]]@("WebAPI")
        PsDscAllowPlainTextPassword = $true
        WebAppPoolName = "UKHO.SourceDocumentService"
        WebSiteName = "UKHO.EventsAndRules"
        BindingHostname = "api.ukho.gov.uk"
        BindingPort = 82
        HttpsBindingPort = 443
        CertThumbprint = "b8a397fad67096c55a811b963b87f13c43385fb0"
        PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"
        WebApplicationName = "SourceDocumentService"
        PhysicalPathWebApplication = "C:\inetpub\UKHO.EventsAndRules\SourceDocumentService"
        WebAppPoolCredentials = $ConfigData.NonNodeData.ServiceCred
        BackConnectionHostNames = "api.ukho.gov.uk"
        MaxEnvelopeSize = 8192
        SetServerCredSSP = $True
        CertificateFile = "HPCPPRDWEB01.Business.UKHO.gov.uk-DscEncryptionCert.cer"         
        Thumbprint = ""
        WebConfigTransformDataObject = @{
            PermittedRoles = "AG_TMLiveSDocSvcApi-DL"
            FilestorePath = "\\business.ukho.gov.uk\\DFS\\AppData\\HDB\\SDRADocuments\\"
            ContentServiceBaseUrl = "http://csp.api.ukho.gov.uk/content/pcp-live/v1/"
            ContentServiceUsername = $ConfigData.NonNodeData.ServiceCred.UserName
            ContentServicePassword = $contentServicePassword
        }
    }
    @{
        NodeName = "HPCPPRDWEB02.business.UKHO.gov.uk"
        PSDscAllowDomainUser = $true
        Roles = [string[]]@("WebAPI")
        PsDscAllowPlainTextPassword = $true
        WebAppPoolName = "UKHO.SourceDocumentService"
        WebSiteName = "UKHO.EventsAndRules"
        BindingHostname = "api.ukho.gov.uk"
        BindingPort = 82
        HttpsBindingPort = 443
        CertThumbprint = "3998327c99dceebd302811225ba57c862b8eedb5"
        PhysicalPathWebSite = "C:\inetpub\UKHO.EventsAndRules"
        WebApplicationName = "SourceDocumentService"
        PhysicalPathWebApplication = "C:\inetpub\UKHO.EventsAndRules\SourceDocumentService"
        WebAppPoolCredentials = $ConfigData.NonNodeData.ServiceCred
        BackConnectionHostNames = "api.ukho.gov.uk"
        MaxEnvelopeSize = 8192
        SetServerCredSSP = $True
        CertificateFile = "HPCPPRDWEB02.Business.UKHO.gov.uk-DscEncryptionCert.cer"         
        Thumbprint = ""
        WebConfigTransformDataObject = @{
            PermittedRoles = "AG_TMLiveSDocSvcApi-DL"
            FilestorePath = "\\business.ukho.gov.uk\\DFS\\AppData\\HDB\\SDRADocuments\\"
            ContentServiceBaseUrl = "http://csp.api.ukho.gov.uk/content/pcp-live/v1/"
            ContentServiceUsername = $ConfigData.NonNodeData.ServiceCred.UserName
            ContentServicePassword = $contentServicePassword
        }
    }
)

$ConfigData.NonNodeData.ServiceAccountSetup = @{
    Domain = "business.ukho.gov.uk"
    ServiceAccountsOU = "OU=Service Accounts,OU=Live,OU=PCP,OU=Services,DC=business,DC=ukho,DC=gov,DC=uk"
    AccountGroups ="AG_PCPLiveIISSVC-GG"
    AddToIISUserGroup = $true
}
