using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System.ComponentModel;

namespace SUKOAuto
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length<2) {
                Console.WriteLine("エラー: [EMIAL] [PASSWORD] <CHANNEL ID>");
                Console.WriteLine("コマンド: ");
                Console.WriteLine("--first-10 : 最初の10個をすこる");
                Console.WriteLine("--suko [N] : 最初のN個をすこる");
                Console.WriteLine("--para [N] : N並列ですこる");
                Console.WriteLine("コマンドはEMIAL、PASSWORD、CHANNEL IDのいずれかの間に入れても構わない。");
                Console.WriteLine("極端例: example@suko.org --suko 10 sukosuko --para 100 UC...");
                Console.WriteLine("推奨例: --suko 10 --para 100 example@suko.org sukosuko UC...");
                Console.WriteLine(" ");
                Console.WriteLine("We won't leak your private!");
                Console.WriteLine("Source code: https://github.com/AnKoushinist/suko-suko-button/tree/master/cui");
                Console.WriteLine("Original Author: Unnamed user in Nan-J");
                Console.WriteLine("Modified by: AnKoushinist");
                Console.WriteLine("Thanks: The holy Hatsune Daishi");
                return;
            }
            SukoSukoOption opt = new SukoSukoOption();
            args = opt.LoadOpt(args);

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

            List<IWebDriver> Chromes = new IWebDriver[opt.parallel].Select(a => new ChromeDriver(ChromeOptions)).Cast<IWebDriver>().ToList();
            IWebDriver Chrome = Chromes[0];

            foreach (IWebDriver SingleChrome in Chromes)
            {
                Console.WriteLine("スレッド{0}: 輪番ログイン中...",Chromes.IndexOf(SingleChrome));
                SukoSukoMachine.Login(SingleChrome, Mail, Pass);
            }
            System.Threading.Thread.Sleep(2000);

            Console.WriteLine("スレッド0: 動画探索中...");
            string[] Movies = SukoSukoMachine.FindMovies(Chrome, Channel);
            if (opt.maxSuko!=-1) {
                Movies = Movies.Take(opt.maxSuko).ToArray();
            }
            List<string>[] MoviesEachThread = new List<string>[Chromes.Count].Select(a=> new List<string>()).ToArray();
            for (int i=0; i<Movies.Length; i++)
            {
                MoviesEachThread[i % MoviesEachThread.Length].Add(Movies[i]);
            }

            BackgroundWorker[] Threads = new BackgroundWorker[Chromes.Count].Select(a => new BackgroundWorker()).ToArray();
            for (int i = 0; i < Threads.Length; i++)
            {
                int Number = i;
                IWebDriver SingleChrome = Chromes[i];
                List<string> LocalMovies=MoviesEachThread[i];
                Threads[i].DoWork += (a, b) =>
                {
                    foreach (string MovieID in LocalMovies)
                    {
                        Console.WriteLine(@"スレッド{0}: {1}すこ！ ({2}/{3})", Number, MovieID, LocalMovies.IndexOf(MovieID), LocalMovies.Count);
                        SukoSukoMachine.Suko(SingleChrome, MovieID);
                    }
                    Console.WriteLine("スレッド{0}: 完了", Number);
                    SingleChrome.Dispose();
                };
                Threads[i].RunWorkerAsync();
            }
            while (Threads.Where(a=>a.IsBusy).Count()!=0) {
                foreach (BackgroundWorker Thread in Threads) {
                    while(Thread.IsBusy);
                }
            }
        }
    }
    
    class SukoSukoMachine
    {
        const string URL_LOGIN = @"https://accounts.google.com/ServiceLogin";
        const string URL_MOVIE = @"https://www.youtube.com/watch?v={0}";
        const string URL_CHANNEL = @"https://www.youtube.com/channel/{0}/videos";

        public static string[] FindMovies(IWebDriver Chrome, string Channel)
        {
            Chrome.Url = string.Format(URL_CHANNEL, Channel);
            ReLogin(Chrome, Chrome.Url);

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
            ReLogin(Chrome, Chrome.Url);

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
                if (Chrome.Url== "https://accounts.google.com/ServiceLogin#identifier")
                {
                    // old login screen
                    Chrome.FindElement(By.Id("Email")).SendKeys(Mail);
                    Chrome.FindElement(By.Id("next")).Click();
                    while (!Chrome.Url.Contains("#password")) ;
                    System.Threading.Thread.Sleep(2000);
                    while (!Chrome.FindElement(By.Name("Passwd")).Displayed) ;
                    Chrome.FindElement(By.Name("Passwd")).SendKeys(Pass);
                    Chrome.FindElement(By.Id("signIn")).Click();
                }
                else
                {
                    // new login screen
                    Chrome.FindElement(By.Id("identifierId")).SendKeys(Mail);
                    Chrome.FindElement(By.Id("identifierNext")).Click();
                    while (!Chrome.Url.Contains("/v2/sl/pwd")) ;
                    System.Threading.Thread.Sleep(2000);
                    while(!Chrome.FindElement(By.Name("password")).Displayed);
                    Chrome.FindElement(By.Name("password")).SendKeys(Pass);
                    Chrome.FindElement(By.Id("passwordNext")).Click();
                }
                System.Threading.Thread.Sleep(3000);
            }
            catch (Exception)
            {
                Console.WriteLine("ログイン失敗: E-mailかパスワードの間違い");
                throw;
            }
        }

        public static void ReLogin(IWebDriver Chrome,string ContinuationURL) {
            if (
               Chrome.FindElements(By.CssSelector("span.yt-uix-button-content")).Count != 0 &&
               Chrome.FindElement(By.CssSelector("span.yt-uix-button-content")).Text == "ログイン"
               )
            {
                // looks like we need to login again here
                Console.WriteLine("再ログイン中...");
                Chrome.FindElement(By.CssSelector("span.yt-uix-button-content")).Click();
                System.Threading.Thread.Sleep(100);
                Chrome.Url = ContinuationURL;
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


    class SukoSukoOption {
        public int parallel=3;
        public int maxSuko=-1;

        public string[] LoadOpt(string[] args) {
            List<string> finalArgs = new List<string>();
            for (int i=0; i<args.Length;i++) {
                switch (args[i].ToLower()) {
                    case "--first-10":
                        maxSuko = 10;
                        break;
                    case "--suko":
                        maxSuko = int.Parse(args[++i]);
                        break;
                    case "--parallel":
                    case "--para":
                    case "--heikou":
                        maxSuko = int.Parse(args[++i]);
                        break;
                    default:
                        finalArgs.Add(args[i]);
                        break;
                }
            }
            return finalArgs.ToArray();
        }
    }
}
