# Portal AAD Auth Guide

## Table of contents

* [Azure Active Directory (AAD) Authorization](#Azure-Active-Directory-(AAD)-Authorization)
    1. [Portal App Service Identity](#1.-Portal-App-Service-Identity)
    2. [Create an App Registration](#2.-Create-an-App-Registration)
    3. [Set App Registration details](#3.-Set-App-Registration-details)
    4. [Copy details from App Registration](#4.-Copy-details-from-App-Registration)
    5. [Grant App Registration permissions](#5.-Grant-App-Registration-permissions)
    6. [Finished](#6.-Finished)
* [Graph API Permissions](#Graph-API-Permissions)
    1. [Set App Registration details](#1.-Set-App-Registration-details)
    2. [Copy details from App Registration](#2.-Copy-details-from-App-Registration)
    3. [Grant App Registration API Permissions](#3.-Grant-App-Registration-API-Permissions)
    4. [Finished](#4.-Finished)

---

## Azure Active Directory (AAD) Authorization
### 1. Portal App Service Identity
Portal App Service's `identity` section and the `system assigned` tab should have a `status` of `on`

### 2. Create an App Registration
Navigate to AAD App Registrations.  
Create a new App Registration with:
* A friendly display `name`, ideally `[APP-NAME]-appreg`  
  **DO NOT USE AN IDENTICAL NAME AS THE APP NAME**
* `Redirect URI` will be set later and can be left blank

### 3. Set App Registration details
Open the new AAD App Registration.  
Navigate to the `Authentication` section.  
Under `Redirect URIs` add:
1. `https://localhost:[PORT-NUMBER]/`
2. `https://localhost:[PORT-NUMBER]/signin-oidc`
3. `https://[APP-NAME].azurewebsites.net/`
4. `https://[APP-NAME].azurewebsites.net/signin-oidc`

Set the `Logout URL` to  
`https://[APP-NAME].azurewebsites.net/signout-oidc`  
Under `Implicit grant`, check the `ID tokens` checkbox.  
Under `Supported account types` make sure that the `Accounts in this organizational directory only` radio button is selected.  
Save the changes.

### 4. Copy details from App Registration
Copy the `Application (client) ID` value from the new App Registration in AAD `Enterprise Applications` and add it to the appropriate config. It will be used as the `ClientId` option in `AzureADOptions`.  
Copy the `Tenant ID` if that has not already been stored in an appropriate config key. It will be used as the `TenantId` in option in `AzureADOptions`.

### 5. Grant App Registration permissions
Add the new App Registration against the subscription in IAM with `contributor` rights.  
Load app site and login with admin priviliges.
The site should hopefully request admin consent, which should be granted.

### 6. Finished
Hopefully the site can now successfully progress beyond the login screen, both locally and in Azure.  
However, if there are subsequent calls to the Graph API, there are more steps to follow in the next section.

---

## Graph API Permissions
### 1. Set App Registration details
Open the new AAD App Registration.  
Navigate to the `Certificates and secrets` section.  
Add a new `client secret`.

### 2. Copy details from App Registration
Copy the `client secret` value from the new App Registration `Certificates and secrets` section and add it to the appropriate config. It will be used as the `ClientAzureAdSecret` option in `MsalAuthenticationProvider` as the `azureAdSecret` parameter.

### 3. Grant App Registration API Permissions
Open the new AAD App Registration.  
Navigate to the `API permissions` section.  
Add a new `Graph API permission` for `application` **NOT** `delegated` with the name `User.Read.All`.  
Once the `preparing consent` button has finished loading, click the button to `grant admin consent`.
There should be five permissions now, all with `green checkmarks` in the `status` column:
| Permission name | Type | Admin consent |
| :--- | :---: | :---: |
| Directory.Read.All | Application | Yes |
| Group.Read.All | Application | Yes |
| openid | Delegated | no |
| profile | Delegated | no |
| User.Read.All | Application | Yes |

### 4. Finished
Hopefully the site can now make calls to the Users section of the Graph API without resulting in an error such as `Authorization_RequestDenied`