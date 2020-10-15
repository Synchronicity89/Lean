namespace AltData {
    
    using AddReference = clr.AddReference;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class TiingoNewsAlgorithm {
        
        static TiingoNewsAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class TiingoNewsAlgorithm
            : QCAlgorithm {
            
            public object aaplCustom;
            
            public Dictionary<string, double> words;
            
            public virtual object Initialize() {
                // Predefine a dictionary of words with scores to scan for in the description
                // of the Tiingo news article
                this.words = new Dictionary<object, object> {
                    {
                        "bad",
                        -0.5},
                    {
                        "good",
                        0.5},
                    {
                        "negative",
                        -0.5},
                    {
                        "great",
                        0.5},
                    {
                        "growth",
                        0.5},
                    {
                        "fail",
                        -0.5},
                    {
                        "failed",
                        -0.5},
                    {
                        "success",
                        0.5},
                    {
                        "nailed",
                        0.5},
                    {
                        "beat",
                        0.5},
                    {
                        "missed",
                        -0.5}};
                this.SetStartDate(2019, 6, 10);
                this.SetEndDate(2019, 10, 3);
                this.SetCash(100000);
                var aapl = this.AddEquity("AAPL", Resolution.Hour).Symbol;
                this.aaplCustom = this.AddData(TiingoNews, aapl).Symbol;
                // Request underlying equity data.
                var ibm = this.AddEquity("IBM", Resolution.Minute).Symbol;
                // Add news data for the underlying IBM asset
                var news = this.AddData(TiingoNews, ibm).Symbol;
                // Request 60 days of history with the TiingoNews IBM Custom Data Symbol
                var history = this.History(TiingoNews, news, 60, Resolution.Daily);
                // Count the number of items we get from our history request
                this.Debug("We got {len(history)} items from our history request");
            }
            
            public virtual object OnData(object data) {
                // Confirm that the data is in the collection
                if (!data.ContainsKey(this.aaplCustom)) {
                    return;
                }
                // Gets the data from the slice
                var article = data[this.aaplCustom];
                // Article descriptions come in all caps. Lower and split by word
                var descriptionWords = article.Description.lower().split(" ");
                // Take the intersection of predefined words and the words in the
                // description to get a list of matching words
                var intersection = new HashSet<object>(this.words.keys()).intersection(descriptionWords);
                // Get the sum of the article's sentiment, and go long or short
                // depending if it's a positive or negative description
                var sentiment = (from i in intersection
                    select this.words[i]).ToList().Sum();
                this.SetHoldings(article.Symbol.Underlying, sentiment);
            }
        }
    }
}
