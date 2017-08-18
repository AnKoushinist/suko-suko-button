using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;

namespace SUKOAuto
{
    class Program
    {
        static void Main(string[] args)
        {
            string Mail = args[0];
            string Pass = args[1];
            string Channel = args[2];

            var ChromeOptions = new ChromeOptions();

            IWebDriver Chrome = new ChromeDriver(ChromeOptions);

            Console.WriteLine("ログイン中...");
            SukoSukoMachine.Login(Chrome, Mail, Pass);
            System.Threading.Thread.Sleep(2000);

            Console.WriteLine("動画探索中...");
            string[] Movies = SukoSukoMachine.FindMovies(Chrome, Channel);
            foreach (string MovieID in Movies)
            {
                Console.WriteLine(@"{0}すこ！", MovieID);
                SukoSukoMachine.Suko(Chrome, MovieID);
            }
            Console.WriteLine("完了");
            Chrome.Dispose();
        }
    }


    class SukoSukoMachine
    {
        const string URL_LOGIN = @"https://accounts.google.com/ServiceLogin/identifier";
        const string URL_MOVIE = @"https://www.youtube.com/watch?v={0}";
        const string URL_CHANNEL = @"https://www.youtube.com/channel/{0}/videos";

        public static string[] FindMovies(IWebDriver Chrome, string Channel)
        {
            Chrome.Url = string.Format(URL_CHANNEL, Channel);

            while (Chrome.PageSource.Contains("もっと読み込む"))
            {
                try
                {
                    Chrome.FindElement(By.XPath("//button[contains(@aria-label, 'もっと読み込む')]")).Click();
                }
                catch { }
                System.Threading.Thread.Sleep(100);
            }

            return RegExp(Chrome.PageSource, @"(?<=<a href=""/watch\?v=)[\dA-Za-z_-]+");
        }

        public static void Suko(IWebDriver Chrome, string MovieID)
        {
            Chrome.Url = string.Format(URL_MOVIE, MovieID);
            System.Threading.Thread.Sleep(500);

            IWebElement SukoBtn = Chrome.FindElement(By.XPath("//button[@title = '低く評価']"));

            Actions action = new Actions(Chrome);
            action.MoveToElement(SukoBtn).Perform();
            action.ClickAndHold().Perform();
            action.MoveToElement(SukoBtn).Perform();
            System.Threading.Thread.Sleep(100);
            action.MoveToElement(SukoBtn).Perform();
            action.Release().Perform();

        }

        public static void Login(IWebDriver Chrome, string Mail, string Pass)
        {
            Chrome.Url = URL_LOGIN;

            Chrome.FindElement(By.Id("identifierId")).SendKeys(Mail);
            Chrome.FindElement(By.Id("identifierNext")).Click();
            System.Threading.Thread.Sleep(1000);
            Chrome.FindElement(By.Name("password")).SendKeys(Pass);
            Chrome.FindElement(By.Id("passwordNext")).Click();

        }

        static string[] RegExp(string Content, string RegStr)
        {
            var listResult = new List<string>();
            var RegExp = new System.Text.RegularExpressions.Regex(RegStr, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            var Matches = RegExp.Matches(Content);

            foreach (System.Text.RegularExpressions.Match Match in Matches)
            {
                listResult.Add(Match.Value);
            }

            return listResult.ToArray();
        }
    }
}
