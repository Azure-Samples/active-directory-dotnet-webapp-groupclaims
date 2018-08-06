---
services: active-directory
platforms: dotnet
author: jmprieur
level: 400
client: .NET 4.6.1 Web App (MVC)
service: ASP.NET Web API
endpoint: AAD V1
---
# Authorization in a web app using Azure AD groups & group claims

![Build badge](https://identitydivision.visualstudio.com/_apis/public/build/definitions/a7934fdd-dcde-4492-a406-7fad6ac00e17/<BuildNumber>/badge)

## About this sample

### Overview
This sample shows how to build an MVC web application that uses Azure AD Groups for authorization.  Authorization in Azure AD can also be done with Application Roles, as shown in [WebApp-RoleClaims-DotNet](https://github.com/Azure-Samples/active-directory-dotnet-webapp-roleclaims). This sample uses the OpenID Connect ASP.Net OWIN middleware and ADAL .Net.

![Overview](./ReadmeFiles/topology.png)

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

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

### Step 4:  Configure the sample to use your Azure AD tenant

In the steps below, "ClientID" is the same as "Application ID" or "AppId".

Open the solution in Visual Studio to configure the projects

#### Configure the service project

1. Open the `WebApp-GroupClaims-DotNet\Web.Config` file
1. Find the app key `ida:ClientId` and replace the existing value with the application ID (clientId) of the `TaskTrackerWebApp-GroupClaims` application copied from the Azure portal.
1. Find the app key `ida:AppKey` and replace the existing value with the key you saved during the creation of the `TaskTrackerWebApp-GroupClaims` app, in the Azure portal.
1. Find the app key `ida:Tenant` and replace the existing value with your Azure AD tenant name.
1. Find the app key `ida:PostLogoutRedirectUri` and replace the existing value with the base address of the TaskTrackerWebApp-GroupClaims project (by default `https://localhost:44322/`).

### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it!  Explore the sample by signing in, navigating to different pages, adding tasks, signing out, etc.  Create several user accounts in the Azure Management Portal, and create tasks as each different user.  Create a Security Group in the Azure Management Portal, add users to it, and share tasks with the security group.

## Deploy this Sample to Azure

To deploy this application to Azure, you will publish it to an Azure Website.

1. Sign in to the [Azure portal](https://portal.azure.com).
2. Click New in the top left hand corner, select Web + Mobile, click on "See All" and choose Web App + SQL, select the hosting plan and region, and give your web site a name, e.g. tasktracker-contoso.azurewebsites.net.  Click Create Web Site.
3. Choose "SQL Database", click on "Create a new database", enter "GroupClaimContext" as the **DB Connection String Name**.
4. Select or create a database server, and enter server login credentials.
5. Once the web app is created, click on it to manage it.  For the purposes of this sample, download the publish profile from Quick Start or from the Dashboard and save it.  Other deployment mechanisms, such as from source control, can also be used.
6. While still in the Azure portal, navigate back to the Azure AD tenant you used in creating this sample.  Under applications, select your Task Tracker application.  Under Settings, update the Sign-On URL and Reply URL fields to the root address of your published application, for example https://tasktracker-contoso.azurewebsites.net/.  Click Save.
7. Switch to Visual Studio and go to the WebApp-GroupClaims-DotNet project.  In the web.config file, update the "PostLogoutRedirectUri" value to the root address of your published application as well.
8. Right click on the project in the Solution Explorer and select Publish.  Under Profile, click Import, and import the publish profile that you just downloaded.
9. On the Connection tab, update the Destination URL so that it is https, for example https://tasktracker-contoso.azurewebsites.net.  Click Next.
10. On the Settings tab, make sure that Enable Organizational Authentication is NOT selected.  Click Publish.
11. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.


## Code Walk-Through

Coming soon.

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

For more information, see ADAL.NET's conceptual documentation:

> Provide links to the flows from the conceptual documentation
> for instance:
- [Application roles](https://docs.microsoft.com/en-us/azure/architecture/multitenant-identity/app-roles)
- [Azure AD Connect sync: Understanding Users, Groups, and Contacts](https://docs.microsoft.com/en-us/azure/active-directory/connect/active-directory-aadconnectsync-understanding-users-and-contacts)
- [Configure Office 365 Groups with on-premises Exchange hybrid](https://docs.microsoft.com/en-us/exchange/hybrid-deployment/set-up-office-365-groups)
- [Recommended pattern to acquire a token](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token#recommended-pattern-to-acquire-a-token)
- [Acquiring tokens interactively in public client applications](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Acquiring-tokens-interactively---Public-client-application-flows)
- [Customizing Token cache serialization](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Token-cache-serialization)

For more information about how OAuth 2.0 protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).