# Fiix Line View
## Small Dotnet 9 app to display Fiix Analytics dahboards on full screens or tv's.  This app manages and persists logins to Fiix to ensure continuous operation. 

## Custom Reports in Real-Time
Display dashboards and looks on your shop floor or office area without the worry of timeouts and sessions expiring.
![alt text](https://fiixpowertools.azurewebsites.net/Images/Looker-AssetsOffline.png "Sample Dashboard")

## Carousel Through Dashboards
This simple app maintains an active connection to your Fiix tenant and automatically cycles through the dashboards you define. Display duration and the number of dashboards are fully customizable to suit your needs.
![alt text](https://fiixpowertools.azurewebsites.net/Images/Looker-MeanTimeDashboard.png "Sample Screen")

## Configuration
Just drop your dashboard URLs and tenant info into a plain text file—simple JSON, and you're set.
![alt text](https://fiixpowertools.azurewebsites.net/Images/Config.png "Sample Screen")

# Config

When you extract the app you will see an file in that folder called appsettings.json, open it in notepad.

### LoginDomain: 
This is the domain of your tenant. Take your URL and enter the name between https:// and .macmms.com

### DashboardURLs: 
This is the list of dashboards you want to display. To get this URL open Fiix Analytics and hold CTRL and click the dashboard with want to display. It will open in a new tab, copy that URL.

### DwellTimeBetweenDashboardsSeconds: 
Set in seconds how long you want the dashboard to display for.

### Username: 
Your Fiix user name.

### RefreshMinutes: 
The number of minutes before the app refreshes credentials with Fiix, this ensures you do not see session time out errors.

To Run The App: Simply open the Persistent-Fiix-Analytics.exe in the folder you extracted the App to. The system will prompt you for your password and start.
If you wish to have the app run automatically you can add Persistent-Fiix-Analytics.exe to the Windows task scheduler and add your password as an argument.
"Persistent-Fiix-Analytics.exe YourPassword" This will run handsfree. 
