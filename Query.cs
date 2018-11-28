using System.Collections.Concurrent;

namespace StockDataHarvester
{
    class Query
    {
        public ConcurrentBag<Term> terms { get; set; }
    }
}
