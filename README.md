WebApp-RBAC-DotNet
==================================

This sample shows how to build an MVC web application that uses Azure AD for sign-in using the OpenID Connect protocol and uses Azure AD Security Groups for role based access control. This sample uses the OpenID Connect ASP.Net OWIN middleware and ADAL .Net.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

##About The Sample
If you would like to get started immediately, skip this section and jump to *How To Run The Sample*. 

This MVC 5 web application is a simple "Task Tracker" application that allows users to create, read, update, and delete tasks.  Within the application, access to certain functionalities is restricted to different subsets of users. For instance, not every user has the ability to create a task.

This kind of access control or authorization is implemented using role based access control (RBAC).  When using RBAC, an administrator grants permissions to roles, not to individual users. The administrator can then assign roles to different users and control who has access to what content and functionality.  Our Task Tracker application defines four *Application Roles*:
- Admin: Has the ability to perform all actions, as well as manage the Application Roles of other users.
- Writer: Has the ability to create tasks.
- Approver: Has the ability to change the status of tasks.
- Observer: Only has the ability to view tasks and their statuses.

It is imporant to make the distinction between these *Application Roles* and the *Directory Roles* that are built into Azure Active Directory.  For a complete list of built-in directory roles, use the [Get-MsolRole](http://technet.microsoft.com/en-us/library/dn194100.aspx) cmdlet.

The application also incorporates group membership for enforcing authorization policies.  In addition to assigning roles directly to users, application Admins can assign roles to Azure AD Security Groups, and assign users to those groups.  In this sample, we manage user membership to Security Groups through the [Azure Management Portal](https://manage.windowsazure.com/), but it can also be accomplished programatically using the [AAD Graph API](http://msdn.microsoft.com/en-us/library/azure/hh974476.aspx).

In order to persist a record of the Application Roles each user and group has been granted, the Task Tracker application stores the mappings of both users and Security Groups to Application Roles in a database.  In addition, it stores tasks that have been created in the database for future access.

Using RBAC with custom Application Roles and Azure Active Directory Security Groups, this application securely enforces authorization policies with simple management of users and groups.



## How To Run The Sample

To run this sample you will need:
- Visual Studio 2013
- An Internet connection
- An Azure subscription (a free trial is sufficient)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [http://wwww.windowsazure.com](http://www.windowsazure.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/AzureADSamples/WebApp-RBAC-DotNet.git`

### Step 2:  Create a user account in your Azure Active Directory tenant

If you already have a user account with Global Administrator rights in your Azure Active Directory tenant, you can skip to the next step.  This sample will not work with a Microsoft account, so if you signed in to the Azure portal with a Microsoft account and have never created a user account in your directory before, you need to do so now, and ensure it has the Global Administrator Directory Role.  If you create an account and want to use it to sign-in to the Azure portal, don't forget to add the user account as a co-administrator of your Azure subscription.

### Step 3:  Register the sample with your Azure Active Directory tenant

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Active Directory in the left hand nav.
3. Click the directory tenant where you wish to register the sample application.
4. Click the Applications tab.
5. In the drawer, click Add.
6. Click "Add an application my organization is developing".
7. Enter a friendly name for the application, for example "TaskTrackerWebApp", select "Web Application and/or Web API", and click next.
8. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44322/`.  NOTE:  It is important, due to the way Azure AD matches URLs, to ensure there is a trailing slash on the end of this URL.  If you don't include the trailing slash, you will receive an error when the application attempts to redeem an authorization code.
9. For the App ID URI, enter `https://<your_tenant_name>/<your_application_name>`, replacing `<your_tenant_name>` with the name of your Azure AD tenant and `<your_application_name>` with the name you chose above.  Click OK to complete the registration.
10. While still in the Azure portal, click the Configure tab of your application.
11. Find the Client ID value and copy it aside, you will need this later when configuring your application.
12. Create a new key for the application.  Save the configuration so you can view the key value.  Save this aside for when you configure the project in Visual Studio.
13. In the Permissions to Other Applications configuration section, ensure that "Read Directory Data" is selected under "Application permissions" and that both "Access your organization's directory" and "Enable sign-on and read user's profiles" are selected under "Delegated permissions."  Save the configuration.
14. Navigate to the Owners tab of your application.
15. Add at least one user as an application owner. The application assigns any user that is an owner the application role of "Admin."  This allows you to login as an admin initially and begin assigning roles to other users.

### Step 4:  Configure the sample to use your Azure AD tenant

1. Open the solution in Visual Studio 2013.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name, i.e. "tasktracker.onmicrosoft.com".
4. Find the app key `ida:ClientId` and replace the value with the Client ID for the application from the Azure portal.
5. Find the app key `ida:AppKey` and replace the value with the key for the application from the Azure portal.
6. If you changed the base URL of the TodoListWebApp sample, find the app key `ida:PostLogoutRedirectUri` and replace the value with the new base URL of the sample.

### Step 6:  Run the sample

Clean the solution, rebuild the solution, and run it.  Explore the sample by signing in, navigating to different pages, adding tasks, signing out, etc.  Create several user accounts in the Azure Management Portal, and assign them different roles using the application owner account you created.  Create a Security Group in the Azure Management Portal, add users to it, and again add roles to it using an Admin account.  Explore the differences between each role throughout the application, namely the Tasks and Roles pages.


## Code Walk-Through

Coming soon.

## Deploy this Sample to Azure

To deploy this application to Azure, you will publish it to an Azure Website.

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Web Sites in the left hand nav.
3. Click New in the bottom left hand corner, select Compute --> Web Site --> Quick Create, select the hosting plan and region, and give your web site a name, e.g. tasktracker-contoso.azurewebsites.net.  Click Create Web Site.
4. Once the web site is created, click on it to manage it.  For the purposes of this sample, download the publish profile from Quick Start or from the Dashboard and save it.  Other deployment mechanisms, such as from source control, can also be used.
5. While still in the Azure management portal, navigate back to the Azure AD tenant you used in creating this sample.  Under applications, select your Task Tracker application.  Under configure, update the Sign-On URL and Reply URL fields to the root address of your published application, for example https://tasktracker-contoso.azurewebsites.net/.  Click Save.
5. Switch to Visual Studio and go to the WebApp-RBAC-DotNet project.  In the web.config file, update the "PostLogoutRedirectUri" value to the root address of your published appliction as well.
6. Right click on the project in the Solution Explorer and select Publish.  Under Profile, click Import, and import the publish profile that you just downloaded.
6. On the Connection tab, update the Destination URL so that it is https, for example https://tasktracker-contoso.azurewebsites.net.  Click Next.
7. On the Settings tab, make sure Enable Organizational Authentication is NOT selected.  Click Publish.
8. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.
