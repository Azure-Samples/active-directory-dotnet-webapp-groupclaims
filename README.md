WebApp-GroupClaims-DotNet
==================================

This sample shows how to build an MVC web application that uses Azure AD Groups for authorization.  Authorization in Azure AD can also be done with Application Roles, as shown in [WebApp-RoleClaims-DotNet](https://github.com/AzureADSamples/WebApp-RoleClaims-DotNet). This sample uses the OpenID Connect ASP.Net OWIN middleware and ADAL .Net.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

##About The Sample

This MVC 5 web application is a simple "Task Tracker" application that allows users to create, read, update, and delete tasks.  Within the application, any user can create a task, and become the owner of any task they create.  As an owner of a task, the user can delete the task and share the task with other users.  Other users are only able to read and update tasks that they own or that have been shared with them.

To enforce authorization on tasks based on sharing, the application uses Azure AD Groups and Group Claims.  Users can share their tasks directly with other users, or with Azure AD Groups (Security Groups or Distribution Lists).  If a task is shared with a group, all members of that group will have read and update access to the task.  The application is able to determine which tasks a user can view based on their group membership, which is indicated by the Group Claims that the application receives on user sign in.  

If you would like to see a sample that enforces Role Based Access Control (RBAC) using Azure AD Application Roles and Role Claims, see [WebApp-RoleClaims-DotNet](https://github.com/AzureADSamples/WebApp-RoleClaims-DotNet).  Azure AD Groups and Application Roles are by no means mutually exclusive - they can be used in tandem to provide even finer grained access control.


## How To Run The Sample

To run this sample you will need:
- Visual Studio 2013
- An Internet connection
- An Azure subscription (a free trial is sufficient)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [https://azure.microsoft.com](https://azure.microsoft.com/en-us/pricing/free-trial/).  All of the Azure AD features used by this sample are available free of charge.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/AzureADSamples/WebApp-GroupClaims-DotNet.git`

### Step 2:  Register the sample with your Azure Active Directory tenant

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Active Directory in the left hand nav.
3. Click the directory tenant where you wish to register the sample application.
4. Click the Applications tab.
5. In the drawer, click Add.
6. Click "Add an application my organization is developing".
7. Enter a friendly name for the application, for example "TaskTrackerWebApp", select "Web Application and/or Web API", and click next.
8. For the sign-on URL and reply URL, enter the base URL for the sample, which is by default `https://localhost:44322/`.  NOTE:  It is important, due to the way Azure AD matches URLs, to ensure there is a trailing slash on the end of this URL.  If you don't include the trailing slash, you will receive an error when the application attempts to redeem an authorization code.
9. For the App ID URI, enter `https://<your_tenant_name>/<your_application_name>`, replacing `<your_tenant_name>` with the name of your Azure AD tenant and `<your_application_name>` with the name you chose above.  Click OK to complete the registration.
10. While still in the Azure portal, click the Configure tab of your application.
11. Find the Client ID value and copy it aside, you will need this later when configuring your application.
12. Create a new key for the application.  Save the configuration so you can view the key value.  Save this key aside, you'll need it shortly as well.
13. In the Permissions to Other Applications configuration section, ensure that both "Read directory data" and "Enable sign-on and read user's profiles" are selected under "Delegated permissions" for "Windows Azure Active Directory"  Save the configuration.

### Step 3: Configure your application to receive group claims

1. While still in the Configure tab of your application, click "Manage Manifest" in the drawer, and download the existing manifest.
2. Edit the downloaded manifest by locating the "groupMembershipClaims" setting, and setting its value to "All" (or to "SecurityGroup" if you are not interested in Distribution Lists).
3. Save and upload the edited manifest using the same "Manage Manifest" button in the portal.
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

1. Open the solution in Visual Studio 2013.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name, i.e. "tasktracker.onmicrosoft.com".
4. Find the app key `ida:ClientId` and replace the value with the Client ID for the application from the Azure portal.
5. Find the app key `ida:AppKey` and replace the value with the key for the application from the Azure portal.
6. If you changed the base URL of the TodoListWebApp sample, find the app key `ida:PostLogoutRedirectUri` and replace the value with the new base URL of the sample.

### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it!  Explore the sample by signing in, navigating to different pages, adding tasks, signing out, etc.  Create several user accounts in the Azure Management Portal, and create tasks as each different user.  Create a Security Group in the Azure Management Portal, add users to it, and share tasks with the security group.

## Deploy this Sample to Azure

To deploy this application to Azure, you will publish it to an Azure Website.

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Web Sites in the left hand nav.
3. Click New in the bottom left hand corner, select Compute --> Web Site --> Quick Create, select the hosting plan and region, and give your web site a name, e.g. tasktracker-contoso.azurewebsites.net.  Click Create Web Site.
4. Once the web site is created, click on it to manage it.  For the purposes of this sample, download the publish profile from Quick Start or from the Dashboard and save it.  Other deployment mechanisms, such as from source control, can also be used.
5. While still in the Azure management portal, navigate back to the Azure AD tenant you used in creating this sample.  Under applications, select your Task Tracker application.  Under configure, update the Sign-On URL and Reply URL fields to the root address of your published application, for example https://tasktracker-contoso.azurewebsites.net/.  Click Save.
5. Switch to Visual Studio and go to the WebApp-GroupClaims-DotNet project.  In the web.config file, update the "PostLogoutRedirectUri" value to the root address of your published application as well.
6. Right click on the project in the Solution Explorer and select Publish.  Under Profile, click Import, and import the publish profile that you just downloaded.
6. On the Connection tab, update the Destination URL so that it is https, for example https://tasktracker-contoso.azurewebsites.net.  Click Next.
7. On the Settings tab, make sure that Enable Organizational Authentication is NOT selected.  Click Publish.
8. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

## Code Walk-Through

Coming soon.
