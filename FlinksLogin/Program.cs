using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using OpenQA.Selenium.Support.UI;

namespace FlinksLogin
{
    class Program
    {
        private static readonly string FlinksUrl = $"https://challenge.flinks.io";
        private static readonly string LocalUrl = $"file:///Users/Aymeric/Desktop/Flinks%20Challenge.html";

        static void Main(string[] args)
        {
            var automatedLoginService = new AutomatedLoginService(FlinksUrl);
            automatedLoginService.Login();
        }
    }
}