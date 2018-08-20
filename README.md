---
services: active-directory
platforms: dotnet
author: jmprieur
level: 400
client: .NET 4.6.1 Web App (MVC)
endpoint: AAD V1
---
# Authorization in a web app using Azure AD groups & group claims

[![Build badge](https://identitydivision.visualstudio.com/_apis/public/build/definitions/a7934fdd-dcde-4492-a406-7fad6ac00e17/630/badge)](https://identitydivision.visualstudio.com/IDDP/_build/latest?definitionId=643)

## About this sample

### Overview
This sample shows how to build an MVC web application that uses Azure AD Groups for authorization.  Authorization in Azure AD can also be done with Application Roles, as shown in [WebApp-RoleClaims-DotNet](https://github.com/Azure-Samples/active-directory-dotnet-webapp-roleclaims). This sample uses the OpenID Connect ASP.Net OWIN middleware and [ADAL.Net](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-libraries).

- An Azure AD Office Hours session covered Azure AD Approles and security groups, featuring this scenario and this sample. Watch the video [Using Security Groups and Application Roles in your apps](https://www.youtube.com/watch?v=V8VUPixLSiM)

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

![Overview](./ReadmeFiles/topology.png)

### Scenario

This MVC 5 web application is a simple "Task Tracker" application that allows users to create, read, update, and delete tasks.  Within the application, any user can create a task, and become the owner of any task they create.  As an owner of a task, the user can delete the task and share the task with other users.  Other users are only able to read and update tasks that they own or that have been shared with them.

To enforce authorization on tasks based on sharing, the application uses Azure AD Groups and Group Claims.  Users can share their tasks directly with other users, or with Azure AD Groups (Security Groups or Distribution Lists).  If a task is shared with a group, all members of that group will have read and update access to the task.  The application is able to determine which tasks a user can view based on their group membership, which is indicated by the Group Claims that the application receives on user sign in.  

If you would like to see a sample that enforces Role Based Access Control (RBAC) using Azure AD Application Roles and Role Claims, see [WebApp-RoleClaims-DotNet](https://github.com/Azure-Samples/active-directory-dotnet-webapp-authz-roleclaims).  Azure AD Groups and Application Roles are by no means mutually exclusive - they can be used in tandem to provide even finer grained access control.


## How to run this sample

To run this sample, you'll need:

- [Visual Studio 2017](https://aka.ms/vsdownload)
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/)
- A user account in your Azure AD tenant. This sample will not work with a Microsoft account (formerly Windows Live account). Therefore, if you signed in to the [Azure portal](https://portal.azure.com) with a Microsoft account and have never created a user account in your directory before, you need to do that now.


### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-groupclaims.git`

> Given that the name of the sample is pretty long, and so are the name of the referenced NuGet pacakges, you might want to clone it in a folder close to the root of your hard drive, to avoid file size limitations on Windows.

### Step 2:  Register the sample with your Azure Active Directory tenant

There is one project in this sample. To register this project, you can:

- either follow the steps in the paragraphs below ([Step 2](#step-2--register-the-sample-with-your-azure-active-directory-tenant) and [Step 3](#step-3--configure-the-sample-to-use-your-azure-ad-tenant))
- or use PowerShell scripts that:
  - **automatically** create for you the Azure AD applications and related objects (passwords, permissions, dependencies)
  - modify the Visual Studio projects' configuration files.

If you want to use this automation, read the instructions in [App Creation Scripts](./AppCreationScripts/AppCreationScripts.md)

#### Register the service app (TaskTrackerWebApp-GroupClaims)

1. In the  **Azure Active Directory** pane, click on **App registrations** and choose **New application registration**.
1. Enter a friendly name for the application, for example 'TaskTrackerWebApp-GroupClaims' and select 'Web app / API' as the *Application Type*.
1. For the *sign-on URL*, enter the base URL for the sample. By default, this sample uses `https://localhost:44322/`.
1. Click **Create** to create the application.
1. In the succeeding page, Find the *Application ID* value and record it for later. You'll need it to configure the Visual Studio configuration file for this project.
1. Then click on **Settings**, and choose **Properties**.
1. For the App ID URI, replace the guid in the generated URI 'https://\<your_tenant_name\>/\<guid\>', with the name of your service, for example, 'https://\<your_tenant_name\>/TaskTrackerWebApp-GroupClaims' (replacing `<your_tenant_name>` with the name of your Azure AD tenant)
1. From the **Settings** | **Reply URLs** page for your application, update the Reply URL for the application to be `https://localhost:44322/`
1. From the Settings menu, choose **Keys** and add a new entry in the Password section:

   - Type a key description (of instance `app secret`),
   - Select a key duration of either **In 1 year**, **In 2 years**, or **Never Expires**.
   - When you save this page, the key value will be displayed, copy, and save the value in a safe location.
   - You'll need this key later to configure the project in Visual Studio. This key value will not be displayed again, nor retrievable by any other means,
     so record it as soon as it is visible from the Azure portal.
1. Configure Permissions for your application. To that extent, in the Settings menu, choose the 'Required permissions' section and then,
   click on **Add**, then **Select an API**, and type `Microsoft Graph` in the textbox. Then, click on  **Select Permissions** and select **Directory.Read.All**.

### Step 3: Configure your application to receive group claims

1. In your application page, click on "Manifest" to open the inline manifest editor.
2. Edit the manifest by locating the "groupMembershipClaims" setting, and setting its value to "All" (or to "SecurityGroup" if you are not interested in Distribution Lists).
3. Save the manifest.
```JSON
{
  ...
  "errorUrl": null,
  "groupMembershipClaims": "All",
  "homepage": "https://localhost:44322/",
  ...
}
```
4. To receive the `groups` claim with the object id of the security groups, make sure that the user accounts you plan to sign-in in is assigned to a few security groups in this AAD tenant.

### Step 4:  Configure the sample to use your Azure AD tenant

In the steps below, "ClientID" is the same as "Application ID" or "AppId".

Open the solution in Visual Studio to configure the projects

#### Configure the service project

1. Open the `WebApp-GroupClaims-DotNet\Web.Config` file
1. Find the app key `ida:ClientId` and replace the existing value with the application ID (clientId) of the `TaskTrackerWebApp-GroupClaims` application copied from the Azure portal.
1. Find the app key `ida:AppKey` and replace the existing value with the key you saved during the creation of the `TaskTrackerWebApp-GroupClaims` app, in the Azure portal.
1. Find the app key `ida:Domain` and replace the existing value with your Azure AD tenant's domain name.
1. Find the app key `ida:PostLogoutRedirectUri` and replace the existing value with the base address of the TaskTrackerWebApp-GroupClaims project (by default `https://localhost:44322/`).

### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it!  Explore the sample by signing in, navigating to different pages, adding tasks, signing out, etc.  Create several user accounts in the Azure Management Portal, and create tasks as each different user.  Create a Security Group in the Azure Management Portal, add users to it, and share tasks with the security group.

Click on `share` link to share a taslk with another user or a security group. 

Click on the user's login name on the top right corner (user@domain.com) to get a list of all the groups and roles that a user is part of. This page fetches the data from [Microsoft Graph](https://graph.microsoft.com).

## Deploy this Sample to Azure

To deploy this application to Azure, you will publish it to an Azure Website.

1. Sign in to the [Azure portal](https://portal.azure.com).
1.  Click **Create a resource** in the top left-hand corner, select **Web + Mobile** --> **Web App**, select the hosting plan and region, and give your web site a name, for example, `TaskTrackerWebApp-GroupClaims-contoso.azurewebsites.net`.  Click Create Web Site.
1.Choose **SQL Database**, click on "Create a new database", enter `GroupClaimContext` as the **DB Connection String Name**.
1. Select or create a database server, and enter server login credentials.
1. Once the web site is created, click on it to manage it.  For this set of steps, download the publish profile by clicking **Get publish profile** and save it.  Other deployment mechanisms, such as from source control, can also be used.
1. Switch to Visual Studio and go to the TaskTrackerWebApp-GroupClaims project.  Right click on the project in the Solution Explorer and select **Publish**.  Click **Import Profile** on the bottom bar, and import the publish profile that you downloaded earlier.
1. Click on **Settings** and in the `Connection tab`, update the Destination URL so that it is https, for example [https://TaskTrackerWebApp-GroupClaims-contoso.azurewebsites.net](https://TaskTrackerWebApp-GroupClaims-contoso.azurewebsites.net). Click Next.
1. On the Settings tab, make sure `Enable Organizational Authentication` is NOT selected.  Click **Save**. Click on **Publish** on the main screen.
1. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

## Processing Groups claim in tokens

### The `groups` claim.
The object id of the security groups the signed in user is member of is returned in the `groups` claim of the token.
```JSON
{
  ...
  "groups": [
    "0bbe91cc-b69e-414d-85a6-a043d6752215",
    "48931dac-3736-45e7-83e8-015e6dfd6f7c",]
  ...
}
```

### Groups overage claim
To ensure that the token size doesn’t exceed HTTP header size limits, Azure AD limits the number of object Ids that it includes in the groups claim.
If a user is member of more groups than the overage limit (150 for SAML tokens, 200 for JWT tokens), then Azure AD does not emit the groups claim in the token. 
Instead, it includes an overage claim in the token that indicates to the application to query the Graph API to retrieve the user’s group membership.

```JSON
{
  ...
  "_claim_names": {
    "groups": "src1"
    },
    {
   "_claim_sources": {
    "src1": {
        "endpoint":"[Graph Url to get this user's group membership from]"
        }
    }    
  ...
}
```
You can use the `BulkCreateGroups.ps1` provided in the [App Creation Scripts](./AppCreationScripts/) folder to help test overage scenarios.

### Support in ASP.NET OWIN middleware libraries 
The asp.net middleware supports roles populated from claims by specifying the claim in the `RoleClaimType` property of `TokenValidationParameters`. 
Since the `groups` claim contains the object ids of the security groups than actual names, the following code will not work. 

```C#
// Startup.Auth.cs
public void ConfigureAuth(IAppBuilder app)         
{             
	app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);             
	app.UseCookieAuthentication(new CookieAuthenticationOptions());             

	//Configure OpenIDConnect, register callbacks for OpenIDConnect Notifications             
	app.UseOpenIdConnectAuthentication(                 
	new OpenIdConnectAuthenticationOptions                 
	{                     
		ClientId = ConfigHelper.ClientId,                     
		Authority = String.Format(CultureInfo.InvariantCulture, ConfigHelper.AadInstance, ConfigHelper.Tenant), 
	            PostLogoutRedirectUri = ConfigHelper.PostLogoutRedirectUri,                     
		TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters                     
		{                         
			ValidateIssuer = false,                          
			RoleClaimType = "groups",                     
		},                     
		
		// [removed for] brevity
    });         
}


// In code..(Controllers & elsewhere)
[Authorize(Roles = “AlicesGroup")]    
// or
User.IsInRole("AlicesGroup"); 

```
You’d have to either write your own IAuthorizationFilter or override User.IsInRole to use Azure AD security groups in your code.

## Code Walk-Through

1. **UserProfileController.cs** - Explore the code in this file on how to fetch a user's directory (App) roles and security group assignments.
1. **AuthenticationHelper.cs** - It has examples of how to obtain access tokens from AAD and how to effectivel;y cache them.
1. **MSGraphClient.cs** - A small implementation of a client for [Microsoft Graph](https://graph.microsoft.com). Includes examples on how to call MS Graph endpoints and how to paginate through results.
1. **TokenHelper.cs** - This class has the code that shown you how to inspect the '_claim_names' and use the value in '_claim_sources' to fetch the security groups when an overage occurs.
1. **TasksController.cs** - Contains a few examples of how to use the security groups in the code.
1. **MSGraphPickerLibrary.js** - A client side javascript library that calls [Microsoft Graph](https://graph.microsoft.com) and fetches Groups and Users.

## Community Help and Support

Use [Stack Overflow](http://stackoverflow.com/questions/tagged/adal) to get support from the community.
Ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.
Make sure that your questions or comments are tagged with [`adal` `dotnet`].

If you find a bug in the sample, please raise the issue on [GitHub Issues](../../issues).

To provide a recommendation, visit the following [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contributing

If you'd like to contribute to this sample, see [CONTRIBUTING.MD](/CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## More information

For more information, see [Azure Active Directory, now with Group Claims and Application Roles] (https://cloudblogs.microsoft.com/enterprisemobility/2014/12/18/azure-active-directory-now-with-group-claims-and-application-roles/)

- [Application roles](https://docs.microsoft.com/en-us/azure/architecture/multitenant-identity/app-roles)
- [Azure AD Connect sync: Understanding Users, Groups, and Contacts](https://docs.microsoft.com/en-us/azure/active-directory/connect/active-directory-aadconnectsync-understanding-users-and-contacts)
- [Configure Office 365 Groups with on-premises Exchange hybrid](https://docs.microsoft.com/en-us/exchange/hybrid-deployment/set-up-office-365-groups)
- [Recommended pattern to acquire a token](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token#recommended-pattern-to-acquire-a-token)
- [Acquiring tokens interactively in public client applications](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Acquiring-tokens-interactively---Public-client-application-flows)
- [Customizing Token cache serialization](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Token-cache-serialization)

For more information about how OAuth 2.0 protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).