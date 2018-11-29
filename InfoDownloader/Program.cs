using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InfoDownloader
{
    class Program
    {
        const string ApiKey = "OjE0NjhjNjRhNWE1ZWE2YmQ4MThjNDM3MzVkY2IxNWM3";
        const string ApiPath = "https://api.intrinio.com/companies";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You must supply a file path as the first argument.");
                return;
            }

            IEnumerable<StockInfo> stockInfos;

            if (!args.Contains("skip"))
            {
                var path = args[0];
                var fileExists = File.Exists(path);
                if (!fileExists)
                {
                    Console.WriteLine("The specified file could not be found.");
                    return;
                }

                var tickers = File.ReadAllLines(path);
                stockInfos = new ConcurrentBag<StockInfo>();
                var count = 0;
                Parallel.ForEach(tickers, () => new HttpClient(), (ticker, loopState, httpClient) =>
                {
                    HttpResponseMessage response;
                    do
                    {
                        response = httpClient.GetAsync($"{ApiPath}?identifier={ticker}&api_key={ApiKey}").GetAwaiter().GetResult();
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            break;
                        }
                        Console.WriteLine(response.StatusCode);
                    }
                    while (!response.IsSuccessStatusCode);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return httpClient;
                    }

                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    try
                    {
                        var stockInfo = JsonConvert.DeserializeObject<StockInfo>(body);
                        ((ConcurrentBag<StockInfo>)stockInfos).Add(stockInfo);
                    }
                    catch (JsonReaderException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(body);
                    }
                    Console.WriteLine(++count);
                    return httpClient;
                }, _ => { });

                var resultsFile = File.CreateText("Results_Json.csv");

                resultsFile.Write(JsonConvert.SerializeObject(stockInfos.AsEnumerable()));
                resultsFile.Flush();
                resultsFile.Close();
            }
            else
            {
                var jsonDoc = File.ReadAllLines("Results_Json.csv")[0];
                stockInfos = JsonConvert.DeserializeObject<IEnumerable<StockInfo>>(jsonDoc);
            }

            var cleanedInfos = stockInfos.AsParallel()
                .Select(stockInfo =>
                {
                    return $"{stockInfo.ticker},{stockInfo.name},{stockInfo.legalName},{stockInfo.sic},{stockInfo.stockExchange},{stockInfo.shortDescription},{stockInfo.longDescription}";
                }).ToList();

            var file = File.CreateText("Results_Cleaned.csv");

            file.WriteLine("ticker,name,legalname,sic,stockexchange,shortDescription,longDescription");
            foreach (var line in cleanedInfos)
            {
                file.WriteLine(line);
            }
            file.Flush();
            file.Close();

            Console.WriteLine(stockInfos.Count());
        }
    }

    public class StockInfo
    {
        private static readonly Regex nonAlphaMatch = new Regex("[^a-zA-Z0-9 ]");
        private string _ticker;
        private string _shortDescription;
        private string _longDescription;
        private string _name;
        private string _sic;
        private string _legalName;
        private string _stockExchange;

        [JsonProperty("ticker")]
        public string ticker { get => _ticker; set => _ticker = nonAlphaMatch.Replace(value ?? "", " "); }

        [JsonProperty("short_description")]
        public string shortDescription { get => _shortDescription; set => _shortDescription = nonAlphaMatch.Replace(value ?? "", " "); }

        [JsonProperty("long_description")]
        public string longDescription { get => _longDescription; set => _longDescription = nonAlphaMatch.Replace(value ?? "", " "); }

        [JsonProperty("name")]
        public string name { get => _name; set => _name = nonAlphaMatch.Replace(value ?? "", " "); }

        [JsonProperty("sic")]
        public string sic { get => _sic; set => _sic = value; }

        [JsonProperty("legal_name")]
        public string legalName { get => _legalName; set => _legalName = nonAlphaMatch.Replace(value ?? "", " "); }

        [JsonProperty("stock_exchange")]
        public string stockExchange { get => _stockExchange; set => _stockExchange = nonAlphaMatch.Replace(value ?? "", " "); }
    }
}
