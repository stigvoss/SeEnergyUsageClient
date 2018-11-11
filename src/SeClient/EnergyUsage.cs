using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace SeClient
{
    public class EnergyUsage : IDisposable
    {
        private readonly HttpClientHandler _handler;
        private readonly HttpClient _client;
        private readonly HtmlParser _html;
        private readonly Uri _baseSelfServiceUrl;
        private readonly Uri _baseApiUrl;

        public string UserName { get; }
        public string Password { get; }

        public EnergyUsage(
            string userName,
            string password,
            string customerId, // 483477
            string siteId, // 44571359
            string baseSelfServiceAddress = "https://www.se.dk/minside",
            string baseApiAddress = "https://webtools3.se.dk/wts")
        {
            _baseSelfServiceUrl = new Uri(baseSelfServiceAddress);
            _baseApiUrl = new Uri(baseApiAddress);

            var cookies = new CookieContainer();
            _handler = new HttpClientHandler() { CookieContainer = cookies };
            _client = new HttpClient(_handler);

            _html = new HtmlParser();

            UserName = userName;
            Password = password;
        }

        public async Task OpenAsync()
        {
            var loginUrl = new Uri(_baseSelfServiceUrl, "/login?returnUrl=%2Fminside");
            var usageUrl = new Uri(_baseSelfServiceUrl, "/energiforbrug/forbrugsvisning");

            var loginResult = await _client.GetStringAsync(loginUrl.AbsoluteUri);
            var loginDom = await _html.ParseAsync(loginResult);

            var token = ReadRequestTokenFrom(loginDom);

            var authenticationBody = EncodeAuthentication(token, UserName, Password);

            var usageResult = await _client.GetStringAsync(usageUrl.AbsoluteUri);
            var usageDom = await _html.ParseAsync(usageResult);

            var sessionId = ReadSessionIdFrom(usageDom);

            AddClientAuthenticationHeaders(sessionId);
        }

        public async Task<object> DownloadUsageAsync()
        {
            var url = new Uri(_baseApiUrl, "/seriesData");
            
            var requestBody = EncodeUsageRequest();

            var apiResult = await _client.PostAsync(_baseApiUrl.AbsoluteUri, requestBody);
            
        }

        private FormUrlEncodedContent EncodeUsageRequest()
        {
            return new FormUrlEncodedContent(new Dictionary<string, string>
            { { "itemId", "SYDEN$483477$44571359$2" },
                { "itemCategory", "SonWinMeter" },
                { "start", "1483225200000" },
                { "end", "1514761200000" },
                { "series[0][seriesId]", "SYDEN$483477$44571359$2$usageConsumption$PurchaseFromNet$Energy" },
                { "series[0][prescaleUnitId]", "kilo@energy_watt" },
                { "series[0][zoomLevelId]", "year_by_months" },
            });
        }

        private void AddClientAuthenticationHeaders(string sessionId)
        {
            _client.DefaultRequestHeaders.Add("Session-Id", sessionId);
            _client.DefaultRequestHeaders.Add("Context-Id", "Site");
            _client.DefaultRequestHeaders.Add("Language-Id", "da-dk");
            _client.DefaultRequestHeaders.Add("Customer-Database-Number", "0");
        }

        private string ReadSessionIdFrom(IHtmlDocument dom)
        {
            const string Pattern = "sessionId: \"(?<sessionId>[a-z0-9\\-]+?)\"";

            var scripts = dom.QuerySelectorAll("script");

            var sessionScript = scripts.Select(script => script.TextContent.Trim())
                .FirstOrDefault(script =>
                    script.StartsWith("webtools"));

            var match = Regex.Match(sessionScript, Pattern);

            if (!match.Success)
            {
                throw new Exception("Session Id not found.");
            }

            return match.Groups["sessionId"].Value;
        }

        private FormUrlEncodedContent EncodeAuthentication(string token, string userName, string password)
        {
            var parameters = new Dictionary<string, string>
                { { "__RequestVerificationToken", token },
                    { "UserName", userName },
                    { "Password", password },
                    { "PersistPassword", "false" }
                };

            return new FormUrlEncodedContent(parameters);
        }

        private string ReadRequestTokenFrom(IHtmlDocument dom)
        {
            const string TokenElementSelector = "input[name='__RequestVerificationToken']";

            var tokenElement = dom.QuerySelector(TokenElementSelector);

            if (tokenElement is object)
            {
                var valueAttribute = tokenElement.Attributes.FirstOrDefault(e => e.Name == "Value");
                return valueAttribute?.Value;
            }

            throw new Exception("Request Verification Token was not found.");
        }

        public void Dispose()
        {
            _handler.Dispose();
            _client.Dispose();
        }
    }
}