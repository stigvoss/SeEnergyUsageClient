using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;

namespace SelfService
{
    public class SeSelfServiceClient : IDisposable
    {
        private readonly HttpClientHandler _handler;
        private readonly HttpClient _client;
        private readonly HtmlParser _html;
        private readonly Uri _baseSelfServiceUrl;
        private readonly Uri _baseApiUrl;

        public string UserName { get; }
        public string Password { get; }

        public bool IsAuthenticated { get; private set; } = false;

        public SeSelfServiceClient(
            string userName,
            string password,
            string baseSelfServiceAddress = "https://www.se.dk",
            string baseApiAddress = "https://webtools3.se.dk")
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Parameter is null or empty.", nameof(userName));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Parameter is null or empty.", nameof(password));
            }

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
            var loginUrl = new Uri(_baseSelfServiceUrl, "/minside/login?returnUrl=%2Fminside");
            var usageUrl = new Uri(_baseSelfServiceUrl, "/minside/energiforbrug/forbrugsvisning");

            var loginResult = await _client.GetStringAsync(loginUrl.AbsoluteUri);
            var loginDom = await _html.ParseAsync(loginResult);

            var token = ReadRequestTokenFrom(loginDom);

            var authenticationBody = EncodeAuthentication(token, UserName, Password);

            await _client.PostAsync(loginUrl, authenticationBody);

            var usageResult = await _client.GetStringAsync(usageUrl.AbsoluteUri);
            var usageDom = await _html.ParseAsync(usageResult);

            var sessionId = ReadSessionIdFrom(usageDom);

            AddClientAuthenticationHeaders(sessionId);

            IsAuthenticated = true;
        }

        public async Task<IEnumerable<TimePeriodUsage>> UsageAsync(
            string customerId,
            string siteId,
            DateTime start,
            DateTime end,
            TimePeriod period = TimePeriod.Default)
        {
            var url = new Uri(_baseApiUrl, "/wts/seriesData");

            var result = await ApiRequestFrom(url,
                () => EncodeUsageRequest(customerId, siteId, start, end, period),
                new []
                {
                    new
                    {
                        datapoints = new TimePeriodUsage[0]
                    }
                }
            );

            return result.FirstOrDefault()?.datapoints;
        }

        public async Task<IEnumerable<Customer>> CustomersAsync()
        {
            var url = new Uri(_baseSelfServiceUrl, "/scom/api/mypage/contactdata?numAddresses=10");

            var result = await ApiRequestFrom(url, null,
                new
                {
                    Customers = new Customer[0]
                }
            );

            return result.Customers;
        }

        private async Task<T> ApiRequestFrom<T>(
            Uri url,
            Func<FormUrlEncodedContent> encodeBody,
            T anonymousType = null)
        where T : class
        {
            if (!IsAuthenticated)
            {
                throw new Exception("The connection has not been opened.");
            }

            string content;
            if (encodeBody is object)
            {
                var requestBody = encodeBody();
                var apiResult = await _client.PostAsync(url.AbsoluteUri, requestBody);
                content = await apiResult?.Content?.ReadAsStringAsync();
            }
            else
            {
                content = await _client.GetStringAsync(url.AbsoluteUri);
            }

            if (anonymousType is object)
            {
                return JsonConvert.DeserializeAnonymousType(content, anonymousType);
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
        }

        private FormUrlEncodedContent EncodeUsageRequest(
            string customerId,
            string siteId,
            DateTime from,
            DateTime to,
            TimePeriod periodType)
        {
            var fromAsUnix = UnixTimeStampFrom(from);
            var toAsUnix = UnixTimeStampFrom(to);

            var zoomLevel = TimePeriods.GetStringFrom(periodType);

            return new FormUrlEncodedContent(new Dictionary<string, string>
            { { "itemId", $"SYDEN${customerId}${siteId}$2" },
                { "itemCategory", "SonWinMeter" },
                { "start", $"{fromAsUnix}" },
                { "end", $"{toAsUnix}" },
                { "series[0][seriesId]", $"SYDEN${customerId}${siteId}$2$usageConsumption$PurchaseFromNet$Energy" },
                { "series[0][prescaleUnitId]", "kilo@energy_watt" },
                { "series[0][zoomLevelId]", zoomLevel },
            });
        }

        private double UnixTimeStampFrom(DateTime time)
        {
            return (time - new DateTime(1970, 1, 1)).TotalMilliseconds;
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
                var valueAttribute = tokenElement.Attributes.FirstOrDefault(e => e.Name.ToLower() == "value");
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