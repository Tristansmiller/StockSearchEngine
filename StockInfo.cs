using Newtonsoft.Json;

namespace StockDataHarvester
{
    class StockInfo
    {
        [JsonProperty("ticker")]
        public string ticker { get; set; }
        [JsonProperty("short_description")]
        public string shortDescription { get; set; }
        [JsonProperty("long_description")]
        public string longDescription { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("sic")]
        public string sic { get; set; }
        [JsonProperty("legal_name")]
        public string legalName { get; set; }
        [JsonProperty("stock_exchange")]
        public string stockExchange { get; set; }
    }
}
