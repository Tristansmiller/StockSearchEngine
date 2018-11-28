using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StockDataHarvester
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            HttpClient httpClient = new HttpClient();
            string APIKey = "OjE0NjhjNjRhNWE1ZWE2YmQ4MThjNDM3MzVkY2IxNWM3";
            string APIPath = "https://api.intrinio.com/companies";
            List<StockInfo> stockInfoList = new List<StockInfo>();
            stockInfoList = initializeStockInfo();
            while (true)
            {
                Console.WriteLine();
                Console.Write("Input a ticker to find similar stocks: ");
                string ticker = Console.ReadLine();
                HttpResponseMessage response = await httpClient.GetAsync(APIPath + "?identifier=" + ticker + "&api_key=" + APIKey);
                string APIResponse = "";
                if (response.IsSuccessStatusCode)
                {
                    APIResponse = await response.Content.ReadAsStringAsync();
                    StockInfo queryStock = JsonConvert.DeserializeObject<StockInfo>(APIResponse);
                    if (queryStock.shortDescription != null)
                    {
                        queryStock.shortDescription = queryStock.shortDescription.Replace(',', ' ');
                        queryStock.shortDescription = queryStock.shortDescription.Replace(':', ' ');
                        queryStock.shortDescription = queryStock.shortDescription.Replace('.', ' ');
                    }
                    if (queryStock.longDescription != null)
                    {
                        queryStock.longDescription = queryStock.longDescription.Replace(',', ' ');
                    }
                    if (queryStock.ticker != null)
                    {
                        queryStock.ticker = queryStock.ticker.Replace(',', ' ');
                    }
                    if (queryStock.name != null)
                    {
                        queryStock.name = queryStock.name.Replace(',', ' ');
                    }
                    if (queryStock.legalName != null)
                    {
                        queryStock.legalName = queryStock.legalName.Replace(',', ' ');
                    }
                    if (queryStock.stockExchange != null)
                    {
                        queryStock.stockExchange = queryStock.stockExchange.Replace(',', ' ');
                    }
                    Console.WriteLine("Finding Results similar to...");
                    Console.WriteLine("STOCK NAME: " + queryStock.name);
                    Console.WriteLine("LEGAL NAME: " + queryStock.legalName);
                    Console.WriteLine("STOCK EXCHANGE: " + queryStock.stockExchange);
                    Console.WriteLine("DESCRIPTION: " + queryStock.shortDescription);
                    outputResults(rankStocksForSimilarity(queryStock, stockInfoList));
                }
                else
                {
                    Console.WriteLine("Couldn't find any stocks that matched that ticker.");
                }
            }
            Console.WriteLine("Done");
        }

        static List<StockInfo> initializeStockInfo()
        {
            List<string> stringStocks = new List<string>(File.ReadAllLines(@".\StockInfo.csv"));
            ConcurrentBag<StockInfo> stockInfoBag = new ConcurrentBag<StockInfo>();
            Parallel.ForEach(stringStocks, stringStock =>
             {
                 try
                 {
                     StockInfo stock = new StockInfo();
                     string[] splitString = stringStock.Split(',');
                     stock.ticker = splitString[0];
                     stock.name = splitString[1];
                     stock.legalName = splitString[2];
                     stock.sic = splitString[3];
                     stock.stockExchange = splitString[4];
                     stock.shortDescription = splitString[5];
                     stock.shortDescription = stock.shortDescription.Replace(',', ' ');
                     stock.longDescription = splitString[6];
                     stockInfoBag.Add(stock);
                 }
                 catch (Exception e)
                 {
                     Console.WriteLine(e);
                 }

             });
            return stockInfoBag.ToList();
        }

        static int calculateDocumentFrequency(string term, List<StockInfo> documents)
        {
            // ConcurrentBag<int> occurrenceCounter = new ConcurrentBag<int>();
            ConcurrentBag<int> documentCounter = new ConcurrentBag<int>();
            Parallel.ForEach(documents, stock =>
            {
                string[] stockDescriptionTokens = stock.shortDescription.Split(' ');
                for (int i = 0; i < stockDescriptionTokens.Length; i++)
                {
                    if (stockDescriptionTokens[i] == term)
                    {
                        documentCounter.Add(1);
                        break;
                    }
                }
                //int counter = 0;
                //   List<string> stockDescriptionTokens = new List<string>(stock.shortDescription.Split(' '));
                //   stockDescriptionTokens.ForEach(token =>
                //   {
                //       if(token == term)
                //       {
                //           //counter++;
                //       }
                //   });
                //  // occurrenceCounter.Add(counter);
                // //  if (counter != 0)
                // //  {
                //       documentCounter.Add(1);
                ////   }
            });
            //   collectionFrequency = occurrenceCounter.Sum();
            return documentCounter.Sum();
            //  return true;
        }

        static int calculateTermFrequency(string term, StockInfo document)
        {
            ConcurrentBag<string> stockDescriptionTokens = new ConcurrentBag<string>(document.shortDescription.Split(' '));
            ConcurrentBag<int> counter = new ConcurrentBag<int>();
            Parallel.ForEach(stockDescriptionTokens, token =>
            {
                if (token == term)
                {
                    counter.Add(1);
                }
            });
            return counter.Sum();
        }

        static double calculateTF_IDFWeight(string term, StockInfo stock, int documentFrequency, int numDocuments)
        {
            double termFreq = 0;
            try
            {
                termFreq = calculateTermFrequency(term, stock);
                if (termFreq != 0 && documentFrequency != 0)
                {
                    return (1 + Math.Log10(termFreq)) * Math.Log10(numDocuments / documentFrequency);
                }
                else
                {
                    return 0;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("TERM FREQ: " + termFreq);
                Console.WriteLine("DOC FREQ: " + documentFrequency);
                Console.WriteLine("STOCK NAME: " + stock.name);
                Console.WriteLine("STOCK DESC: " + stock.shortDescription);
                Console.WriteLine("TERM: " + term);
                Console.WriteLine();
                Console.WriteLine(e);
                throw e;
            }
        }

        static Query initializeQueryVector(StockInfo stock, List<StockInfo> documents)
        {
            Query query = new Query();
            query.terms = new ConcurrentBag<Term>();
            List<string> queryTokens = new List<string>(stock.shortDescription.Split(' '));
            int numDocuments = documents.Count();
            Parallel.ForEach(queryTokens, token =>
            {
                if (token != "" && token != "or" && token != "and" && token != "the" && token != "is" && token != "a" && token != "that" && token != "this" && token != "for")
                {
                    // Console.WriteLine("Creating a term object for token: " + token);
                    Term currTerm = new Term();
                    currTerm.term = token;
                    try
                    {
                        currTerm.documentFrequency = calculateDocumentFrequency(token, documents);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(token);
                        Console.WriteLine("Failed to calculate Document Frequency");
                    }
                    try
                    {
                        currTerm.TF_IDFWeight = calculateTF_IDFWeight(token, stock, currTerm.documentFrequency, numDocuments);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(token);
                        Console.WriteLine(currTerm.documentFrequency);
                        Console.WriteLine("Failed to calculate TF_IDFWeight");
                    }
                    try
                    {
                        query.terms.Add(currTerm);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(token);
                        Console.WriteLine(currTerm.documentFrequency);
                        Console.WriteLine(currTerm.TF_IDFWeight);
                        Console.WriteLine("Failed to add to bag.");
                        Console.WriteLine(e);
                    }

                }
            });
            return query;
        }

        static double calculateDocumentSimilarity(Query query, StockInfo stock, int numDocuments, List<StockInfo> documents)
        {
            ConcurrentBag<(double, double)> TF_IDFWeightProducts = new ConcurrentBag<(double, double)>();
            double magnitude = 0;
            Parallel.ForEach(query.terms, term =>
            {
                //  Console.WriteLine(term.term);
                double documenttfidf = calculateTF_IDFWeight(term.term, stock, term.documentFrequency, numDocuments);
                magnitude += documenttfidf * documenttfidf;
                TF_IDFWeightProducts.Add((documenttfidf, term.TF_IDFWeight));
            });
            magnitude = Math.Sqrt(magnitude);
            double sum = 0;
            IEnumerator<(double, double)> enumerator = TF_IDFWeightProducts.GetEnumerator();
            while (enumerator.MoveNext())
            {
                sum += ((enumerator.Current.Item1 / magnitude) * enumerator.Current.Item2);
            }
            return sum;
        }

        static List<(StockInfo, double)> rankStocksForSimilarity(StockInfo queryStock, List<StockInfo> stocks)
        {
            Console.WriteLine("Initializing Query Vector.");
            Query query = initializeQueryVector(queryStock, stocks);
            int numDocuments = stocks.Count();
            ConcurrentBag<(StockInfo, double)> rankedStocks = new ConcurrentBag<(StockInfo, double)>();
            Parallel.ForEach(stocks, stock =>
            {
                //Console.WriteLine("Ranking stock ticker: " + stock.ticker);
                rankedStocks.Add((stock, calculateDocumentSimilarity(query, stock, numDocuments, stocks)));
            });
            List<(StockInfo, double)> ascRankedStocks = rankedStocks.ToList();
            ascRankedStocks = ascRankedStocks.OrderBy(obj => obj.Item2).Reverse().ToList();
            return ascRankedStocks;
        }

        static void outputResults(List<(StockInfo, double)> rankedStocks)
        {
            Console.WriteLine("Results:");
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine(i + ": " + rankedStocks[i].Item1.ticker + " " + rankedStocks[i].Item1.name + " " + rankedStocks[i].Item1.shortDescription);
                Console.WriteLine("SCORE: " + rankedStocks[i].Item2);
                Console.WriteLine();
            }
        }
        //static void retrieveAllStock()
        //{
        //    for (int k = 1; k < tickerList.Count(); k++)
        //    {
        //        string el = tickerList.ElementAt(k);
        //        HttpResponseMessage response = await httpClient.GetAsync(APIPath + "?identifier=" + el + "&api_key=" + APIKey);
        //        string APIResponse = "";
        //        if (response.IsSuccessStatusCode)
        //        {
        //            APIResponse = await response.Content.ReadAsStringAsync();
        //            StockInfo StockObj = JsonConvert.DeserializeObject<StockInfo>(APIResponse);

        //            sw.WriteLine(StockObj.ticker + "," + StockObj.name + "," + StockObj.legalName + "," + StockObj.sic + "," + StockObj.stockExchange + "," + StockObj.shortDescription + "," + StockObj.longDescription);
        //            // Console.WriteLine(StockObj.ticker + " " + StockObj.shortDescription + " " + StockObj.longDescription + " " + StockObj.name + " " + StockObj.sic + " " + StockObj.legalName + " " + StockObj.stockExchange);
        //        }

        //    };
        //    sw.Close();
        //}
    }
}
