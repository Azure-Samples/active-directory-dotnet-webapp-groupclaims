---
services: active-directory
platforms: dotnet
author: jmprieur
---

Authorization in a web app using Azure AD groups & group claims
==================================

This sample shows how to build an MVC web application that uses Azure AD Groups for authorization.  Authorization in Azure AD can also be done with Application Roles, as shown in [WebApp-RoleClaims-DotNet](https://github.com/Azure-Samples/active-directory-dotnet-webapp-authz-roleclaims). This sample uses the OpenID Connect ASP.Net OWIN middleware and ADAL .Net.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

##About The Sample

This MVC 5 web application is a simple "Task Tracker" application that allows users to create, read, update, and delete tasks.  Within the application, any user can create a task, and become the owner of any task they create.  As an owner of a task, the user can delete the task and share the task with other users.  Other users are only able to read and update tasks that they own or that have been shared with them.

To enforce authorization on tasks based on sharing, the application uses Azure AD Groups and Group Claims.  Users can share their tasks directly with other users, or with Azure AD Groups (Security Groups or Distribution Lists).  If a task is shared with a group, all members of that group will have read and update access to the task.  The application is able to determine which tasks a user can view based on their group membership, which is indicated by the Group Claims that the application receives on user sign in.  

If you would like to see a sample that enforces Role Based Access Control (RBAC) using Azure AD Application Roles and Role Claims, see [WebApp-RoleClaims-DotNet](https://github.com/Azure-Samples/active-directory-dotnet-webapp-authz-roleclaims).  Azure AD Groups and Application Roles are by no means mutually exclusive - they can be used in tandem to provide even finer grained access control.


## How To Run The Sample

To run this sample you will need:
- Visual Studio 2017
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, please see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/) 
- A user account in your Azure AD tenant. This sample will not work with a Microsoft account, so if you signed in to the Azure portal with a Microsoft account and have never created a user account in your directory before, you need to do that now.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-groupclaims.git`


> Warning: Because the name of the project is pretty long, you might want to clone the repo into a folder which is close to the root
of your hard disk (probably not under \Users\xxxxxxxx\Source\Repos\something). This will avoid you to get "Fully Qualified Name Less Than 260 Characters "
error messages when you build (or restore the NuGet packages.)

### Step 2:  Register the sample with your Azure Active Directory tenant

1. Sign in to the [Azure portal](https://portal.azure.com).
2. On the top bar, click on your account and under the **Directory** list, choose the Active Directory tenant where you wish to register your application.
3. Click on **More Services** in the left hand nav, and choose **Azure Active Directory**.
4. Click on **App registrations** and choose **Add**.
5. Enter a friendly name for the application, for example 'TaskTrackerWebApp' and select 'Web Application and/or Web API' as the Application Type. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44322/`. NOTE:  It is important, due to the way Azure AD matches URLs, to ensure there is a trailing slash on the end of this URL.  If you don't include the trailing slash, you will receive an error when the application attempts to redeem an authorization code. Click on **Create** to create the application.
6. While still in the Azure portal, choose your application, click on **Settings** and choose **Properties**.
7. Find the Application ID value and copy it to the clipboard.
8. In the same page, change the 'Logout URL' to `https://localhost:44322/Account/EndSession`.  This is the default single sign out URL for this sample.
9. For the App ID URI, enter `https://<your_tenant_name>/<your_application_name>`, replacing `<your_tenant_name>` with the name of your Azure AD tenant and `<your_application_name>` with the name you chose above.
10. From the Settings menu, choose **Keys** and add a key - select a key duration of either 1 year or 2 years. When you save this page, the key value will be displayed, copy and save the value in a safe location - you will need this key later to configure the project in Visual Studio - this key value will not be displayed again, nor retrievable by any other means, so please record it as soon as it is visible from the Azure Portal.
11. Configure Permissions for your application - in the Settings menu, choose the 'Required permissions' section, click on **Add**, then **Select an API**, and select 'Microsoft Graph' (this is the Graph API). Then, click on  **Select Permissions** and select 'Read directory data' and 'Sign in and read user profile'.

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

1. Open the solution in Visual Studio 2017.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name, i.e. "tasktracker.onmicrosoft.com".
4. Find the app key `ida:ClientId` and replace the value with the Client ID for the application from the Azure portal.
5. Find the app key `ida:AppKey` and replace the value with the key for the application from the Azure portal.
6. If you changed the base URL of the sample, find the app key `ida:PostLogoutRedirectUri` and replace the value with the new base URL of the sample.

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
