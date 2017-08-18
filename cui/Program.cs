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
            if (args.Length<2) {
                Console.WriteLine("エラー: [EMIAL] [PASSWORD] <CHANNEL ID>");
                Console.WriteLine("We won't leak your private!");
                Console.WriteLine("Source code: https://github.com/AnKoushinist/suko-suko-button/tree/master/cui");
                return;
            }
            string Mail = args[0];
            string Pass = args[1];

            string Channel;
            if (args.Length >= 3)
            {
                Channel = args[2];
            }
            else
            {
                /* TODO: make it only to type */
                Console.WriteLine("チャンネルIDがありません。以下から選択または入力:");
                List<KeyValuePair<string, string>> ids = new List<KeyValuePair<string, string>>();
                ids.Add(new KeyValuePair<string, string>("ヒカル", "UCaminwG9MTO4sLYeC3s6udA"));
                ids.Add(new KeyValuePair<string, string>("ラファエル", "UCI8U2EcQDPwiQmQMBOtjzKA"));
                ids.Add(new KeyValuePair<string, string>("禁断ボーイズ", "UCvtK7490fPF0TacbsvQ2H3g"));
                ids.Add(new KeyValuePair<string, string>("ラファエルサブ", "UCgQgMOBZOJ1ZDtCZ4hwP1uQ"));
                ids.Add(new KeyValuePair<string, string>("ヒカルゲームズ", "kinnpatuhikaru"));
                ids.Add(new KeyValuePair<string, string>("オッドアイ(ピンキー)", "UCRN_Yde2b5G1-5nEeIhcOTw"));
                ids.Add(new KeyValuePair<string, string>("禁断ボーイズサブ", "UCgY7ZuKqLG_QSScSkPxe1NA"));
                ids.Add(new KeyValuePair<string, string>("テオくん", "UCj6_0tBpVpmyYSGu6f-uKqw"));
                ids.Add(new KeyValuePair<string, string>("かす", "UC1fYrot9lgMstv7vX0BnjnQ"));
                ids.Add(new KeyValuePair<string, string>("ぷろたん", "UCl4e200EZm7NXq_iaYSXfeg"));
                ids.Add(new KeyValuePair<string, string>("スカイピース", "UC8_wmm5DX9mb4jrLiw8ZYzw"));
                ids.Add(new KeyValuePair<string, string>("イニ", "UC5VZjrV5x9J9mTyGODzu0dQ"));
                ids.Add(new KeyValuePair<string, string>("楠ろあ", "UCvS01-HQ57pnIjP4lkp58zw"));
                ids.Add(new KeyValuePair<string, string>("ねお", "UClPLW-9Nfbvf76ksj-4c1kQ"));
                ids.Add(new KeyValuePair<string, string>("ピンキー妹", "UCsTM1roCxoot1-03EO5zQxg"));
                foreach (KeyValuePair<string,string> kvp in ids) {
                    Console.WriteLine(@"No. {0} {1} {2}",ids.IndexOf(kvp),kvp.Key,kvp.Value);
                }
                string mem = Console.ReadLine();
                int select = -1;
                if (int.TryParse(mem,out select)) {
                    mem = ids[select].Value;
                }
                Channel = mem;
            }


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
            if (
                Chrome.FindElements(By.CssSelector("span.yt-uix-button-content")).Count!=0&&
                Chrome.FindElement(By.CssSelector("span.yt-uix-button-content")).Text=="ログイン"
                ) {
                // looks like we need to login again here
                Console.WriteLine("再ログイン中...");
                Chrome.FindElement(By.CssSelector("span.yt-uix-button-content")).Click();
                System.Threading.Thread.Sleep(100);
                Chrome.Url = string.Format(URL_CHANNEL, Channel);
            }

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
            if (SukoBtn.GetCssValue("display")=="none") {
                // already downvoted
                return;
            }

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
            try
            {
                Chrome.FindElement(By.Id("identifierId")).SendKeys(Mail);
                Chrome.FindElement(By.Id("identifierNext")).Click();
                while (Chrome.Url.Contains("/v2/sl/pwd")) ;
                System.Threading.Thread.Sleep(2000);
                Chrome.FindElement(By.Name("password")).SendKeys(Pass);
                Chrome.FindElement(By.Id("passwordNext")).Click();
                System.Threading.Thread.Sleep(2000);
                while (Chrome.Url.Contains("myaccount.google.com")) ;
            }
            catch (Exception)
            {
                Console.WriteLine("ログイン失敗: E-mailかパスワードの間違い");
                throw;
            }
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
