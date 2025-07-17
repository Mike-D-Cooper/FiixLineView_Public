using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Reflection;
using System.Security;

namespace FiixLineView
{
    internal class Program
    {
        private static System.Timers.Timer _timerCycleDashboards;
        private static System.Timers.Timer _timer;
        private static System.Timers.Timer _monitorTimer;
        private static IWebDriver _driver;

        private static int _currentDashboardIndex = 0;
        private static List<string> _dashboardURLs;
        private static string password = "";

        static void Main(string[] args)
        {
            DisplyLogoAnddisclaimer();            

            if (args.Length == 0)
            {
                Console.WriteLine("Missing password as a launch argument");
                Console.WriteLine("Enter your Fiix password here:");
                //password = Console.ReadLine();
                password = ReadPassword();
            }
            else
            {
                password = args[0];
            }

            StartChromeAutomation();
        }

        static void StartChromeAutomation()
        {
            // Build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfiguration config = builder.Build();
            
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-fullscreen");
            options.AddArgument("--disable-notifications");
            options.AddExcludedArgument("enable-automation"); // Remove "Chrome is being controlled by automated software" message
            options.AddArgument("--disable-save-password-bubble"); // Disable password manager prompt
            options.AddUserProfilePreference("credentials_enable_service", false);
            options.AddUserProfilePreference("profile.password_manager_enabled", false);
            _driver = new ChromeDriver(options);

            int refreshMinutes = 1;

            int.TryParse(config["RefreshMinutes"], out refreshMinutes);

            // Set up the timer to call RunAutomatedDashboard every 30 minutes
            _timer = new System.Timers.Timer(refreshMinutes * 60 * 1000); // 30 minutes in milliseconds
            _timer.Elapsed += (sender, e) => RunAutomatedDashboard(config);
            _timer.AutoReset = true;
            _timer.Enabled = true;

            int dashboardDwellSeconds = 60;
            int.TryParse(config["DwellTimeBetweenDashboardsSeconds"], out dashboardDwellSeconds);
            // Set up the timer to call CycleDashboards every specified minutes
            _timerCycleDashboards = new System.Timers.Timer(dashboardDwellSeconds * 1000); // Dwell time in milliseconds
            _timerCycleDashboards.Elapsed += (sender, e) => CycleDashboards(config, _driver);
            _timerCycleDashboards.AutoReset = true;
            _timerCycleDashboards.Enabled = false;

            // Set up the timer to monitor the Chrome window state
            _monitorTimer = new System.Timers.Timer(dashboardDwellSeconds / 2 * 1000); // Check every minute
            _monitorTimer.Elapsed += (sender, e) => MonitorChromeWindow();
            _monitorTimer.AutoReset = true;
            _monitorTimer.Enabled = true;

            try
            {
                RunAutomatedDashboard(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Handle application exit
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            // Keep the application running
            Console.WriteLine("Press [Enter] to exit the program.");
            Console.ReadLine();
        }

        static void RunAutomatedDashboard(IConfiguration config)
        {
            _timerCycleDashboards.Enabled = false;



            // Read configuration values
            string loginURL = "https://" + config["LoginDomain"] + ".macmms.com";
            _dashboardURLs = config.GetSection("DashboardURLs").Get<List<string>>();
            string username = config["Username"];
            //string password = config["Password"];

            Console.WriteLine("Start");

            _driver.Navigate().GoToUrl(loginURL + "/logout");
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            Thread.Sleep(3000);
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
            _driver.Navigate().GoToUrl(loginURL);

            Thread.Sleep(3000);
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            _driver.FindElement(By.CssSelector("fiix-input.form-input > input:nth-child(1)")).SendKeys(username);
            _driver.FindElement(By.CssSelector(".fiix-form-field-container > input:nth-child(1)")).SendKeys(password);

            _driver.FindElement(By.CssSelector(".primary-button")).Click();

            Thread.Sleep(5000);

            _driver.FindElement(By.CssSelector(".pk-icon-analytics > span:nth-child(1)")).Click();

            Thread.Sleep(3000);

            if (_dashboardURLs.Count == 1)
            {
                _driver.Navigate().GoToUrl(_dashboardURLs.FirstOrDefault() + "&force_refresh=true");
            }
            else
            {
                _driver.Navigate().GoToUrl(_dashboardURLs[_currentDashboardIndex] + "&force_refresh=true");
                Console.WriteLine("Displaying URL: " + _dashboardURLs[_currentDashboardIndex]);
                _timerCycleDashboards.Enabled = true;
            }
        }

        static void CycleDashboards(IConfiguration config, IWebDriver driver)
        {
            try
            {
                _currentDashboardIndex = (_currentDashboardIndex + 1) % _dashboardURLs.Count;
                driver.Navigate().GoToUrl(_dashboardURLs[_currentDashboardIndex]);

                Console.WriteLine("Displaying URL: " + _dashboardURLs[_currentDashboardIndex]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error controlling chrome.  " + e.Message);
            }
        }

        static void MonitorChromeWindow()
        {
            try
            {
                // Check if the driver is still valid by getting the current URL
                var currentUrl = _driver.Url;
            }
            catch (WebDriverException)
            {
                // If an exception is thrown, restart the automation process
                Console.WriteLine("Chrome window has crashed or closed. Restarting automation...");
                OnProcessExit(null, null);
                StartChromeAutomation();
            }
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            // Stop the timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            // Stop the timer
            if (_timerCycleDashboards != null)
            {
                _timerCycleDashboards.Stop();
                _timerCycleDashboards.Dispose();
            }

            // Stop the monitor timer
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
                _monitorTimer.Dispose();
            }

            // Close and dispose the driver
            if (_driver != null)
            {
                _driver.Quit();
                _driver.Dispose();
            }

            Console.WriteLine("Application exiting...");
        }

        static string ReadPassword()
        {
            string password = string.Empty;
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(intercept: true); // Do not display the key
                if (key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*"); // Display a masking character
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine(); // Move to the next line after Enter is pressed
            return password;
        }

        static void DisplyLogoAnddisclaimer()
        {
            // Display ASCII logo from LogoACSII.txt if it exists
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogoACSII.txt");
            if (File.Exists(logoPath))
            {
                string logo = File.ReadAllText(logoPath);
                Console.WriteLine(logo);
                Console.WriteLine();
            }

            // Print the full version number, including pre-release labels
            var informationalVersion = Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            Console.WriteLine($"Fiix LineView - Version {informationalVersion}");
            Console.WriteLine();
            // Change the color to red for the disclaimer
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("DISCLAIMER:");
            Console.WriteLine("This application is provided 'as-is', without any express or implied warranty.");
            Console.WriteLine("Use it at your own risk. The authors are not responsible for any damage or data loss.");

            // Reset the color back to the default
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
