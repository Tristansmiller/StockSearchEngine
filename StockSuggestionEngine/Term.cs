namespace StockSearchEngine
{
    class Term
    {
        public string term { get; set; }
        public double TF_IDFWeight { get; set; }
        public int documentFrequency { get; set; }
    }
}
