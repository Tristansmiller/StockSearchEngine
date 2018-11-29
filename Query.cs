using System.Collections.Concurrent;

namespace StockDataHarvester
{
    class Query
    {
        public string ticker {get; set;}
        public ConcurrentBag<Term> terms { get; set; }
    }
}
