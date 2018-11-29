using System.Collections.Concurrent;

namespace StockSearchEngine
{
    class Query
    {
        public string ticker {get; set;}
        public ConcurrentBag<Term> terms { get; set; }
    }
}
