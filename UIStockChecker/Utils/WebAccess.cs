using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using UIStockChecker.Models;

namespace UIStockChecker.Utils
{
    internal class WebAccess
    {
        private const string CONNECT = "https://store.ui.com/collections/unifi-connect";
        private const string PROTECT = "https://store.ui.com/collections/unifi-protect";
        private const string PROTECTEA = "https://store.ui.com/collections/early-access/ea-protect";
        private const string ACCESSORIESEA = "https://store.ui.com/collections/early-access/ea-accessories";

        private static List<string> urls = new List<string> { CONNECT, PROTECT, PROTECTEA, ACCESSORIESEA };

        public static List<string> GetUrls()
        {
            return urls;
        }

        public string CheckStock(string url, string device)
        {
            return DownloadURL(url).Contains("Sold Out") ? device + ": Sold Out" : device + ": In Stock";
        }

        public static string GetCookie(string username, string password)
        {
            // Get UBIC Auth Cookie
            string url = "https://sso.ui.com/api/sso/v1/login";
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer, AllowAutoRedirect = false };
            var client = new HttpClient(handler);
            string credentials = "{\"user\":\"" + username + "\"," + "\"password\":\"" + password + "\"}";
            var content = new StringContent(credentials, Encoding.UTF8, "application/json");

            var cookieArray = new string[0];
            var arrayCookieResult = "";

            try
            {
                var result = client.PostAsync(url, content).Result;
                var cookies = result.Headers.GetValues("Set-cookie");

                foreach (var key in cookies)
                {
                    arrayCookieResult = key;
                    break;
                }

                cookieArray = arrayCookieResult.Split(';');
                var ubic_auth = cookieArray[0].Split("=");

                var cookieObj = new Cookie
                {
                    Name = ubic_auth[0],
                    Value = ubic_auth[1],
                    Domain = ".ui.com"
                };

            // Get location redirect, auto redirect has to be disabled in order to get the property
            cookieContainer.Add(cookieObj);
            url = "https://sso.ui.com/api/sso/v1/shopify_login?region=us";
            content = new StringContent("", Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36");
            result = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;
            var location = result.Headers.Location;

            // Get the secure session id cookie
            client.DefaultRequestHeaders.Add("path", location.ToString().Substring(20));
            result = client.GetAsync(location, HttpCompletionOption.ResponseHeadersRead).Result;
            cookies = result.Headers.GetValues("Set-cookie");

            foreach (var key in cookies)
            {
                arrayCookieResult = key;
                break;
            }
        }
            catch(Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }

            return cookieArray + "; " + arrayCookieResult.Split(';')[0];
        }

        public static string DownloadURL(string url)
        {
            return DownloadURL(url, "");
        }

        public static string DownloadURL(string url, string cookie)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json").Build();

            var username = config["Username"];
            var password = config["Password"];

            var data = "";

            WebClient wb = new WebClient();
            if (cookie.Length == 0)
            {
                cookie = GetCookie(username, password);
            }
            wb.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36");
              wb.Headers.Add(HttpRequestHeader.Cookie, cookie);
            try
            {
                data = wb.DownloadString(url);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
            wb.Dispose();
            return data;
        }
        public static List<Item> ProcessURL(string data)
        {
            var items = new List<Item>();
            try
            {
                int start = data.IndexOf("sectionTag: '");
                if (start == -1) return items;
                int end = data.IndexOf("</script>", start);
                if (end == -1) return items;
                var subData = data.Substring(start, end - start);
                start = subData.IndexOf("a  href");
                end = 0;

                while (start > 0)
                {
                    if (end > 0)
                    {
                        start = subData.IndexOf("a  href", end);
                    }
                    else
                    {
                        start = subData.IndexOf("a  href");
                    }

                    if (start == -1) break;

                    end = subData.IndexOf("</a>", start);

                    var item = ParseDiv(subData.Substring(start, end - start));

                    if (item.Name != null && item.Name.Length > 0)
                    {
                        items.Add(item);
                    }
                }
            }
            catch(Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }

            return items;
        }

        private static Item ParseDiv(string div)
        {
            var item = new Item();

            int start = div.IndexOf("a  href=\"") + 9;
            int end = div.IndexOf("\"", start);
            var href = div.Substring(start, end - start);

            start = div.IndexOf("<span class=\"link\">") + 19;
            end = div.IndexOf("</span>", start);
            var productName = div.Substring(start, end - start);

            start = div.IndexOf("<span>") + 6;
            end = div.IndexOf("</span>", start);
            var cost = div.Substring(start, end - start);

            start = div.IndexOf("background-image: url(/") + 24;
            end = div.IndexOf(")", start);
            var image = "https://" + div.Substring(start, end - start);

            bool soldOut = (div.Contains("Sold Out") || div.Contains("Coming Soon"));

            if (!productName.Contains("Network") && !productName.Contains("Cloud"))
            {
                item.Name = productName;
                item.Url = "https://store.ui.com" + href;
                item.Price = cost;
                item.InStock = !soldOut;
                item.ImageUrl = image;
            }

            return item;
        }
    }
}