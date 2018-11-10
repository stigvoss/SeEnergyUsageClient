using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Parser.Html;

namespace SeEnergyReporting
{
    class Program
    {
        private const string _address = "https://www.se.dk/minside/login?returnUrl=%2Fminside";

        static async Task Main(string[] args)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            var client = new HttpClient(handler);

            var loginPage = await client.GetStringAsync(_address);

            var parser = new HtmlParser();

            var loginPageDom = await parser.ParseAsync(loginPage);

            var tokenElement = loginPageDom.QuerySelector("input[name='__RequestVerificationToken']");

            var value = tokenElement.Attributes["value"].Value;

            var dictionary = new Dictionary<string, string>
                { { "__RequestVerificationToken", value },
                    { "UserName", "" },
                    { "Password", "" },
                    { "PersistPassword", "false" }
                };

            await client.PostAsync(_address, new FormUrlEncodedContent(dictionary));

            var usagePage = await client.GetStringAsync("https://www.se.dk/minside/energiforbrug/forbrugsvisning");
            var usagePageDom = await parser.ParseAsync(usagePage);

            var scripts = usagePageDom.QuerySelectorAll("script");

            var sessionScript = scripts.Select(script => script.TextContent.Trim())
                .FirstOrDefault(script => script.StartsWith("webtools"));

            var match = Regex.Match(sessionScript, "sessionId: \"(?<sessionId>[a-z0-9\\-]+?)\"");
            var sessionId = match.Groups["sessionId"].Value;

            client.DefaultRequestHeaders.Add("Session-Id", sessionId);
            client.DefaultRequestHeaders.Add("Context-Id", "Site");
            client.DefaultRequestHeaders.Add("Language-Id", "da-dk");
            client.DefaultRequestHeaders.Add("Customer-Database-Number", "0");

            var apiResult = await client.PostAsync("https://webtools3.se.dk/wts/seriesData", new FormUrlEncodedContent(new Dictionary<string, string>
            { { "itemId", "SYDEN$483477$44571359$2" },
                { "itemCategory", "SonWinMeter" },
                { "start", "1483225200000" },
                { "end", "1514761200000" },
                { "series[0][seriesId]", "SYDEN$483477$44571359$2$usageConsumption$PurchaseFromNet$Energy" },
                { "series[0][prescaleUnitId]", "kilo@energy_watt" },
                { "series[0][zoomLevelId]", "year_by_months" },
            }));

            var contentStream = await apiResult.Content.ReadAsStreamAsync();
            var content = await new StreamReader(contentStream).ReadToEndAsync();
        }
    }
}