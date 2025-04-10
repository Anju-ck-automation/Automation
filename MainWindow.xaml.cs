using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Windows;
using ClosedXML.Excel;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace CookieComparerWPF
{
    public class CookieDetail
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Expiry { get; set; }
        public string Description { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

                private readonly Dictionary<string, List<string>> countryUrls = new()
                {
                    { "hu", new List<string> { "https://preprod.nextdirect.com/hu/hu", "https://preprod.nextdirect.com/hu/en" } },
            { "fi", new List<string> { "https://preprod.next.fi/fi", "https://preprod.next.fi/en" } },
            { "lu", new List<string> { "https://preprod.next.lu/fr", "https://preprod.next.lu/en" } },
            { "be", new List<string> { "https://preprod.nextdirect.com/be/fr", "https://preprod.nextdirect.com/be/en" } },
            { "lv", new List<string> { "https://preprod.next.com.lv/ru", "https://preprod.next.com.lv/en" } },
            { "cz", new List<string> { "https://preprod.nextdirect.com/cz/cs", "https://preprod.nextdirect.com/cz/en" } },
            { "fr", new List<string> { "https://preprod.nextdirect.com/fr/fr", "https://preprod.nextdirect.com/fr/en" } },
            { "id", new List<string> { "https://preprod.nextdirect.com/id/id", "https://preprod.nextdirect.com/id/en" } },
            { "nl", new List<string> { "https://preprod.nextdirect.com/nl/nl", "https://preprod.nextdirect.com/nl/en" } },
            { "si", new List<string> { "https://preprod.next.si/sl", "https://preprod.next.si/en" } },
            { "qa", new List<string> { "https://preprod.next.qa/ar", "https://preprod.next.qa/en" } },
            { "ro", new List<string> { "https://preprod.next.ro/ro", "https://preprod.next.ro/en" } },
            { "ua", new List<string> { "https://preprod.next.ua/uk", "https://preprod.next.ua/en" } },
            { "de", new List<string> { "https://preprod.next.de/de", "https://preprod.next.de/en" } },
            { "lt", new List<string> { "https://preprod.next.lt/ru", "https://preprod.next.lt/en" } },
            { "at", new List<string> { "https://preprod.next.at/de", "https://preprod.next.at/en" } },
            { "pl", new List<string> { "https://preprod.next.pl/pl", "https://preprod.next.pl/en" } },
            { "se", new List<string> { "https://preprod.next.se/sv", "https://preprod.next.se/en" } },
            { "dk", new List<string> { "https://preprod.nextdirect.com/dk/da", "https://preprod.nextdirect.com/dk/en" } },
            { "es", new List<string> { "https://preprod.next.es/es", "https://preprod.next.es/en" } },
            { "sk", new List<string> { "https://preprod.nextdirect.com/sk/sk", "https://preprod.nextdirect.com/sk/en" } },
            { "gr", new List<string> { "https://preprod.nextdirect.com/gr/el", "https://preprod.nextdirect.com/gr/en" } },
            { "bg", new List<string> { "https://preprod.nextdirect.com/bg/bg", "https://preprod.nextdirect.com/bg/en" } },
            { "hr", new List<string> { "https://preprod.nextdirect.com/hr/hr", "https://preprod.nextdirect.com/hr/en" } },
            { "it", new List<string> { "http://preprod.nextdirect.com/it/it", "http://preprod.nextdirect.com/it/en" } },
            { "tr", new List<string> { "https://preprod.nextdirect.com/tr/tr", "https://preprod.nextdirect.com/tr/en" } },
            { "pt", new List<string> { "https://preprod.nextdirect.com/pt/pt", "https://preprod.nextdirect.com/pt/en" } },
            { "eg", new List<string> { "https://preprod.nextdirect.com/eg/ar", "https://preprod.nextdirect.com/eg/en" } },
            { "ee", new List<string> { "https://preprod.next.com.ee/ru", "https://preprod.next.com.ee/en" } },
            { "cy", new List<string> { "https://preprod.nextdirect.com/cy/el", "https://preprod.nextdirect.com/cy/en" } }
                };

        private void CompareCookies_Click(object sender, RoutedEventArgs e)
        {
            string countryCode = (CountryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLower();

            if (string.IsNullOrWhiteSpace(countryCode) || !countryUrls.ContainsKey(countryCode))
            {
                MessageBox.Show("Please select a valid country code.");
                return;
            }

            ResultText.Text = $"Fetching cookies for {countryCode}...";

            var preprodUrls = countryUrls[countryCode];
            var prodUrls = preprodUrls.Select(u => u.Replace("preprod", "www")).ToList();

            var prodCookiesMap = new Dictionary<string, List<CookieDetail>>();
            var preprodCookiesMap = new Dictionary<string, List<CookieDetail>>();

            for (int i = 0; i < preprodUrls.Count; i++)
            {
                string variant = preprodUrls[i].Split('/').Last(); // e.g. "fr", "en"
                preprodCookiesMap[variant] = GetDetailedCookies(preprodUrls[i]);
                prodCookiesMap[variant] = GetDetailedCookies(prodUrls[i]);
            }

            ExportToExcel(prodCookiesMap, preprodCookiesMap);
            ResultText.Text = "✅ Excel file saved to your Desktop.";
        }
        private List<CookieDetail> GetDetailedCookies(string url)
        {
            var options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-dev-shm-usage");

            var cookieDetails = new List<CookieDetail>();

            using (var driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl(url);

                try
                {
                    var manuallyManageBtn = driver.FindElements(By.Id("onetrust-pc-btn-handler"));
                    if (manuallyManageBtn.Count > 0)
                        manuallyManageBtn[0].Click();
                }
                catch { }

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                var cookieDetailButtons = driver.FindElements(By.XPath("//button[contains(@class,'category-host-list-handler')]")).ToList();

                foreach (var button in cookieDetailButtons)
                {
                    try
                    {
                        string sectionName = button.FindElement(By.XPath("parent::div/parent::div/h4")).Text;
                        button.Click();
                        wait.Until(d => d.FindElement(By.Id("ot-pc-lst")));

                        var cookieSections = driver.FindElements(By.ClassName("ot-host-item")).ToList();
                        foreach (var section in cookieSections)
                        {
                            section.Click();
                            var details = section.FindElements(By.ClassName("ot-host-info"));

                            foreach (var detail in details)
                            {
                                try
                                {
                                    string name = detail.FindElement(By.ClassName("ot-c-name")).Text;
                                    string type = detail.FindElement(By.ClassName("ot-c-host")).Text;
                                    string expiry = detail.FindElement(By.ClassName("ot-c-duration")).Text;
                                    string description = detail.FindElement(By.ClassName("ot-c-description")).Text;

                                    cookieDetails.Add(new CookieDetail
                                    {
                                        Name = name,
                                        Type = type,
                                        Expiry = expiry,
                                        Description = description
                                    });
                                }
                                catch { continue; }
                            }
                        }

                        var backbutton = driver.FindElement(By.Id("ot-back-arw"));
                        backbutton.Click();
                        Thread.Sleep(5000);
                        wait.Until(d => d.FindElement(By.Id("ot-pc-content")));
                    }
                    catch { continue; }
                }
            }

            return cookieDetails;
        }

        private void ExportToExcel(Dictionary<string, List<CookieDetail>> prodMap, Dictionary<string, List<CookieDetail>> preprodMap)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Cookie Comparison");

            // Header row
            ws.Cell("A1").Value = "Language";
            ws.Cell("B1").Value = "Cookie Name";
            ws.Cell("C1").Value = "Field";
            ws.Cell("D1").Value = "Production";
            ws.Cell("E1").Value = "Preproduction";
            ws.Cell("F1").Value = "Status";

            int row = 2;

            foreach (var variant in prodMap.Keys)
            {
                var prod = prodMap[variant];
                var preprod = preprodMap[variant];

                var allCookies = prod.Select(c => c.Name)
                                     .Union(preprod.Select(c => c.Name))
                                     .Distinct();

                foreach (var name in allCookies)
                {
                    var prodCookie = prod.FirstOrDefault(c => c.Name == name);
                    var preprodCookie = preprod.FirstOrDefault(c => c.Name == name);

                    CompareField(ws, ref row, variant, name, "Type", prodCookie?.Type, preprodCookie?.Type);
                    CompareField(ws, ref row, variant, name, "Expiry", prodCookie?.Expiry, preprodCookie?.Expiry);
                    CompareField(ws, ref row, variant, name, "Description", prodCookie?.Description, preprodCookie?.Description);
                }
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CookieComparison.xlsx");
            workbook.SaveAs(path);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void CompareField(IXLWorksheet ws, ref int row, string language, string cookieName, string field, string prodVal, string preprodVal)
        {
            string status = prodVal == preprodVal ? "✅ Match" : "❌ Different";
            ws.Cell(row, 1).Value = language;
            ws.Cell(row, 2).Value = cookieName;
            ws.Cell(row, 3).Value = field;
            ws.Cell(row, 4).Value = prodVal ?? "❌ Missing";
            ws.Cell(row, 5).Value = preprodVal ?? "❌ Missing";
            ws.Cell(row, 6).Value = status;

            if (status == "❌ Different")
            {
                ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.LightPink;
            }

            row++;
        }
    }
}
