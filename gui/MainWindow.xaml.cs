using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SukoGUI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string Mail = text_acc.GetLineText(0);
            string Pass = text_pass.GetLineText(0);
            string Channel = textbox.GetLineText(0);

            var ChromeOptions = new ChromeOptions();
            ChromeOptions.AddArgument("--no-sandbox");//add
            ChromeOptions.AddArgument("--start-maximized");

            IWebDriver Chrome = new ChromeDriver(ChromeOptions);


            Console.WriteLine("ログイン中...");
            SukoSukoMachine.Login(Chrome, Mail, Pass);
            System.Threading.Thread.Sleep(2000);


            Console.WriteLine("動画探索中...");
            string[] Movies = SukoSukoMachine.FindMovies(Chrome, Channel, Int32.Parse(push_interval.GetLineText(0)));

            var cookies = Chrome.Manage().Cookies.AllCookies;

            foreach (string MovieID in Movies)
            {
                Console.WriteLine(@"{0}すこ！", MovieID);
                SukoSukoMachine.Suko(Chrome, MovieID, cookies);
            }
            Console.WriteLine("完了");
            Console.ReadLine();
            Chrome.Dispose();
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }
    }


    class SukoSukoMachine
    {
        const string URL_LOGIN = @"https://accounts.google.com/ServiceLogin/identifier";
        const string URL_MOVIE = @"https://www.youtube.com/watch?v={0}";
        const string URL_CHANNEL = @"https://www.youtube.com/channel/{0}/videos";

        public static string[] FindMovies(IWebDriver Chrome, string Channel, int interval)
        {
            Chrome.Url = string.Format(URL_CHANNEL, Channel);
            var Movies = new List<string>();


            Actions action = new Actions(Chrome);
            while (Chrome.PageSource.Contains("もっと読み込む"))
            {
                try
                {
                    action.SendKeys(Keys.PageDown);
                    Chrome.FindElement(By.XPath("//button[contains(@aria-label, 'もっと読み込む')]")).Click();
                    System.Threading.Thread.Sleep(interval);
                }
                catch { }
            }
            Movies.AddRange(RegExp(Chrome.PageSource, @"(?<=<a href=""/watch\?v=)[\dA-Za-z_-]+"));

            return Movies.ToArray();
        }

        public static void Suko(IWebDriver Chrome, string MovieID, IReadOnlyCollection<Cookie> cookies)
        {
            //Chrome.Url = string.Format(URL_MOVIE, MovieID);
            foreach (Cookie cookie in cookies) { Chrome.Manage().Cookies.AddCookie(cookie); }
            Chrome.Navigate().GoToUrl(string.Format(URL_MOVIE, MovieID));
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
