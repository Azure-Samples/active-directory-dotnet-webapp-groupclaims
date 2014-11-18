WebApp-GroupClaims-DotNet
==================================
[![Gitter](https://badges.gitter.im/Join Chat.svg)](https://gitter.im/AzureADSamples/WebApp-GroupClaims-DotNet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This sample shows how to build an MVC web application that uses Azure AD Groups for authorization.  Authorization in Azure AD can also be done with Application Roles, as shown in [WebApp-RoleClaims-DotNet](https://github.com/AzureADSamples/WebApp-RoleClaims-DotNet). This sample uses the OpenID Connect ASP.Net OWIN middleware and ADAL .Net.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

##About The Sample

This MVC 5 web application is a simple "Task Tracker" application that allows users to create, read, update, and delete tasks.  Within the application, access to certain functionality is restricted to subsets of users. For instance, not every user has the ability to create a task.

This kind of access control or authorization is implemented using role based access control (RBAC).  When using RBAC, an administrator grants permissions to roles, not to individual users. The administrator can then assign roles to different users and control who has access to what content and functionality.  

This application implements RBAC based on Group Membership, using Azure AD's Group Claims feature.  Another approach is to use Azure AD Application Roles and Role Claims, as shown in [WebApp-RoleClaims-DotNet](https://github.com/AzureADSamples/WebApp-RoleClaims-DotNet).  Azure AD Groups and Application Roles are by no means mutually exclusive - they can be used in tandem to provide even finer grained access control.

It is imporant to make the distinction between these *Application Roles*, *Azure AD Application Roles*, and *Azure AD Directory Roles*.  In this scenario, *Azure AD Application Roles* are not incorporated.  Instead, the application defines and manages its own set of *Application Roles,* maintaining a record of role assignment in its own database.

Our Task Tracker application defines four *Application Roles*:
- Admin: Has the ability to perform all actions, as well as manage the Application Roles of other users.
- Writer: Has the ability to create tasks.
- Approver: Has the ability to change the status of tasks.
- Observer: Only has the ability to view tasks and their statuses.

Application Admins can assign roles to individual users, as well as to both Azure AD Security Groups and Distribution Lists.  In this sample, user/group membership is managed via the [Azure Management Portal](https://manage.windowsazure.com/), but it can also be accomplished programatically using the [AAD Graph API](http://msdn.microsoft.com/en-us/library/azure/hh974476.aspx).

Using RBAC with Azure Active Directory Security Groups and Azure Active Directory Distribution Lists, this application securely enforces authorization policies with simple management of users and groups.



## How To Run The Sample

To run this sample you will need:
- Visual Studio 2013
- An Internet connection
- An Azure subscription (a free trial is sufficient)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [http://wwww.windowsazure.com](http://www.windowsazure.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/AzureADSamples/WebApp-GroupClaims-DotNet.git`

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
12. Create a new key for the application.  Save the configuration so you can view the key value.  Save this key aside, you'll need it shortly as well.
13. In the Permissions to Other Applications configuration section, ensure that both "Access your organization's directory" and "Enable sign-on and read user's profiles" are selected under "Delegated permissions" for "Windows Azure Active Directory"  Save the configuration.

### Step 4: Configure your application to receive group claims

1. While still in the Configure tab of your application, click "Manage Manifest" in the drawer, and download the existing manifest.
2. Edit the downloaded manifest by locating the "groupMemebershipClaims" setting, and setting its value to "All" (or to "SecurityGroup" if you are not interested in Distribution Lists).
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

### Step 5: Bootstrap your application by creating an Admin user

1. While still in the Azure Portal, navigate to the Owners tab of your application.
1. Add at least one user as an application owner. The application assigns any user that is an owner the application role of "Admin."  This allows you to login as an admin initially and begin assigning roles to other users.

### Step 6:  Configure the sample to use your Azure AD tenant

1. Open the solution in Visual Studio 2013.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name, i.e. "tasktracker.onmicrosoft.com".
4. Find the app key `ida:ClientId` and replace the value with the Client ID for the application from the Azure portal.
5. Find the app key `ida:AppKey` and replace the value with the key for the application from the Azure portal.
6. If you changed the base URL of the TodoListWebApp sample, find the app key `ida:PostLogoutRedirectUri` and replace the value with the new base URL of the sample.

### Step 7:  Run the sample

Clean the solution, rebuild the solution, and run it!  Explore the sample by signing in, navigating to different pages, adding tasks, signing out, etc.  Create several user accounts in the Azure Management Portal, and assign them different roles using the application owner account you created.  Create a Security Group in the Azure Management Portal, add users to it, and again add roles to it using an Admin account.  Explore the differences between each role throughout the application, namely the Tasks and Roles pages.

## Deploy this Sample to Azure

To deploy this application to Azure, you will publish it to an Azure Website.

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Web Sites in the left hand nav.
3. Click New in the bottom left hand corner, select Compute --> Web Site --> Quick Create, select the hosting plan and region, and give your web site a name, e.g. tasktracker-contoso.azurewebsites.net.  Click Create Web Site.
4. Once the web site is created, click on it to manage it.  For the purposes of this sample, download the publish profile from Quick Start or from the Dashboard and save it.  Other deployment mechanisms, such as from source control, can also be used.
5. While still in the Azure management portal, navigate back to the Azure AD tenant you used in creating this sample.  Under applications, select your Task Tracker application.  Under configure, update the Sign-On URL and Reply URL fields to the root address of your published application, for example https://tasktracker-contoso.azurewebsites.net/.  Click Save.
5. Switch to Visual Studio and go to the WebApp-GroupClaims-DotNet project.  In the web.config file, update the "PostLogoutRedirectUri" value to the root address of your published appliction as well.
6. Right click on the project in the Solution Explorer and select Publish.  Under Profile, click Import, and import the publish profile that you just downloaded.
6. On the Connection tab, update the Destination URL so that it is https, for example https://tasktracker-contoso.azurewebsites.net.  Click Next.
7. On the Settings tab, make sure Enable Organizational Authentication is NOT selected.  Click Publish.
8. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

## Code Walk-Through

This section will help you understand the important sections of the sample and how to create the sample from scratch.

#### Get Started

1. Open up Visual Studio 2013, and create a new ASP.NET Web Application.  In the New Project dialog, select MVC, and Change Authentication to "No Authentication." Click OK to create your project.
2. In the project properties, Set SSL Enabled to be True.  Note the SSL URL.
3. Right click on the Project, select Properties --> Web, and set the Project Url to be the SSL URL from above.
4. Add the following NuGets to your project: `Microsoft.Owin.Security.OpenIdConnect`, `Microsoft.Owin.Security.Cookies`, `EntityFramework`, `Microsoft.Owin.Host.SystemWeb`, `Microsoft.IdentityModel.Clients.ActiveDirectory`.   Add the AAD GraphAPI client library NuGet (`Microsoft.Azure.ActiveDirectory.GraphClient`).

#### Enable Users to Sign-In

1. A good place to start is with authentication.  In `Views\Shared` add a MVC5 Partial Page `_LoginPartial.cshtml`.  Replace the contents of the file with the contents of the file of same name from the sample.
2. Also in `Views\Shared`, replace the contents of `_Layout.cshtml` with the code from the sample.  This will light up the `_LoginParital` view from above, ensure that only Admins can view the role management page, and include some of the javascript necessary for later.
3. In `Controllers`, add a new empty controller and name it AccountController.  Replace its implementation with the sample's.  Don't worry about the `WebAppGroupClaimsDotNet` namespace for now, you'll do a global search and replace later.  This controller handles authentication when the user clicks on 'Sign In' or 'Sign Out' in our `_LoginPartial` view.
4. Right-click on the project, select Add --> Class.  In the Add Class dialog, search for "OWIN". Select "OWIN Startup Class" from the results, and name your new OWIN Startup Class `Startup.cs`.  Remove the `.App_Start` portion of the namespace.  You can replace the implementation of this class with the one from the sample - but all you need to do here is change the class declaration to a partial class, and call the ConfigureAuth method.
5. In the `App_Start` folder, create a class `Startup.Auth.cs`.  Replace the code for the `Startup` class with the code from the sample.  This class uses the OWIN middleware for authenticating the user to AAD, by sending and receiving messages according to the OpenIDConnect protocol.  If you would like to see this authentication in action, check out our [WebApp-OpenIDConnect-DotNet](https://github.com/AzureADSamples/WebApp-OpenIDConnect-DotNet) sample.  This Startup class also contains much of the logic necessary for implementing RBAC - but we'll come back to that shortly.

#### Build the Database Schema

1. First, in `Models`, create three new classes - `RoleMapping.cs`, `Task.cs`, and `TokenCacheEntry.cs`.  These will serve as the all of the data models our application needs to persist data.  Copy each of their implementations from the sample.  In `Task.cs` you can see each task has an associated TaskID, TaskText, and Status.  Similarly, a RoleMapping object contains an ID, an ObjectID, and a Role.  The RoleMapping object represents a tie between an AAD object (a user or a group) and an application role (Admin, Approver, Writer, Observer), and will be used to persist a record of application role grants.  Lastly, the TokenCacheEntry object is used to persist AAD access tokens needed for calling the AAD Graph API on a per-user basis.
2. Next, create a new folder in your project called `DAL`, and within it add a new class `GroupClaimContext.cs`, copying the sample's implementation.  This class will be used by Entity Framework 6 to construct the database schema, which you can see contains a table for RoleMappings, Tasks, and TokenCacheEntries.
3. Add two new classes to the `DAL` folder called `TasksDbHelper` and `RolesDbHelper`, and copy in their implementation from the sample.  These classes handle all reads and writes to the database for both Tasks and RBAC data.
4. Also create another new folder called `Utils`, and within create a new class called `TokenDbCache`, and copy in the sample's code.  This class handles all reads and writes to the database for AAD Access Tokens. 

#### Create the Task Tracker

1. To create the actual Task Tracking part of the app, add an empty controller to `Controllers` called TasksController.  Copy the implementation from the sample.  You'll see that this controller reads tasks from and writes tasks to the underlying database.  It uses both the `[Authorize]` attribute and the `IsInRole()` method to ensure that the user attempting to read and write tasks has been granted the necessary privilges.
2. Add the view for the Tasks page, by creating an empty view in the `Views\Tasks` folder called Index.  Copy its implementation from the sample.  You can see that this view uses the `IsInRole()` method extensively to enforce RBAC, ensuring that only the correct users can see the UI for manipulating tasks.

#### Create the Role Management Page

1. Now its time to tackle assigning and granting roles to users.  First, create a new empty controller called `RolesController` and copy in the implementation.  You'll see that the `RolesController` has actions for reading role mappings from the database, assigning a new role, and removing existing roles.
2. Create the corresponding `Index` view in `Views\Roles` by copying its implementation from the sample. 

#### Do a Little Housekeeping

1. Before examining how each of these pieces comes together to enforce RBAC, a few other items need to be included in the project.  First, replace the implementation of the `App_Start\BundleConfig.cs` file with that from the sample.
2. Similarly, replace the `Content\Site.css` file with that of the sample.
3. Create a new empty controller called `ErrorController`, and two views in `Views\Error` called `ShowError.cshtml` and `Reauth.cshtml`.  Copy all of their implementations from the sample - they are simply used for displaying various error messages throughout the application.
4. Replace the `Controllers\HomeController.cs` and `Views\Home\About.cshtml` files with the sample code.  The About page has been adjusted to provide information about the currently signed-in user.  Feel free to delete the `Views\Home\Contact.cshtml` file.
5. Create a new class in `Utils` called `GraphHelper.cs`, and copy the implementation from the sample.  This class handles much of the interaction with the GraphAPI, including acquiring tokens for the Graph and getting object information based on object Id's.
5. Add a new javascript file to `Scripts` called `AadPickerLibrary.js`, and copy the code from the same file in the sample.  This file is a small js library that is used to select users and groups from a tenant in AAD.  In this app, it is used to select users and groups for assignment to roles in the role management page.
6. Add a new class to the root directory of your project called `AuthorizeAttribute.cs`, and copy its implementation as well.  This class helps the MVC framework differentiate between a request that is Forbidden (user is authenticated, but has not been granted access) and Unauthorized (user is not authenticated), ensuring proper app behavior on page redirects.
7. Lastly, you need to provide the application with some specifics about your app's registration in the Azure Management Portal.  Create two classes in `Utils` called `Globals.cs` and `ConfigHelper.cs`, and copy in the code from the sample.  These classes pull in various values from the `web.config` file that are needed for signing the user in, acquiring access tokens, calling the AAD Graph API, and so on.  In `web.config`, in the `<appSettings>` tag, create keys for `ida:ClientId`, `ida:AppKey`, `ida:AADInstance`, `ida:Tenant`, `ida:PostLogoutRedirectUri`, `ida:GraphApiVersion` and `ida:GraphUrl` and set the values accordingly.  For the public Azure AD, the value of `ida:AADInstance` is `https://login.windows.net/{0}`, the value of `ida:GraphApiVersion` is `1.22-preview`, and the value of `ida:GraphUrl` is `https://graph.windows.net`.
8. Finally, do a global search for `WebAppGroupClaimsDotNet` and replace it with the namespace of your application.  This will ensure that your classes do not contain the namespace of the sample app.

#### Run the RBAC App

Build the solution and run the app! Be sure you've followed the above steps in 'How To Run The Sample' in order to make sure your app is configured correctly in the Azure Management Portal.

#### How Does It All Work?

Beyond copying and pasting code, how does this app implement RBAC?  It begins with a role assignment.  As you saw in creating the app, assigning a role to a user a group via `RolesController.cs` creates a `RoleMapping.cs` object with a the user or group's ObjectId and the role they've been assigned.  

When a user logs into the app, the `AuthorizationCodeRecieved` callback in `Startup.Auth.cs` is fired.  This callback first aquires an access token from AAD for calling the AAD Graph API, using the Active Directory Authentication Library (ADAL).  ADAL automatically caches the access token for you, using the cache implementation you provided in `TokenDbCache.cs`.  In other parts of the application, when the app needs to call the Graph API for information about users and groups, ADAL is also used to automatically fetch the access token from the cache, or request a new access token if the cached one is expired.

The `AuthorizationCodeReceived` callback then queries the Graph API using the access token it just acquired.  It retrieves a list of AAD Security Groups that the signed-in user is a member of.

It then queries the underlying database for any existing `RoleMapping` objects that contain either the ObjectId of the user or the ObjectId of one of the user's groups.  As it finds matching `RoleMapping` objects, it adds corresponding Role Claims to the `ClaimsIdentity` of the user.

As one last step, the callback queries the GraphAPI once more to retreive the list of 'Application Owners' from AAD.  It grants any users that are Application Owners the role of Admin, so that when you first run the app, at least one user has Admin access and can begin granting access to other users.

When `AuthorizationCodeReceieved` returns, the OWIN middleware eventually redirects the user to a page within the app.  Each page within the application, as you saw in creating the app, enforces RBAC by using the `[Authorize]` attribute or the `IsInRole()` method.  Both check for the existence of a corresponding Role Claim in the user's `ClaimsIdentity`.

By granting the correct role claims to the user on sign-in, the application can strictly enforce RBAC and ease access management using AAD Security Groups and application-specific roles.
