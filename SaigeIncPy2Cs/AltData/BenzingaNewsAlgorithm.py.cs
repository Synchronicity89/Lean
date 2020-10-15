namespace AltData {
    
    using AddReference = clr.AddReference;
    
    using datetime = datetime.datetime;
    
    using timedelta = datetime.timedelta;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class BenzingaNewsAlgorithm {
        
        static BenzingaNewsAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
            AddReference("QuantConnect.Common");
        }
        
        public class BenzingaNewsAlgorithm
            : QCAlgorithm {
            
            public object lastTrade;
            
            public Dictionary<string, double> words;
            
            public virtual object Initialize() {
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
                this.lastTrade = new datetime(1, 1, 1);
                this.SetStartDate(2018, 6, 5);
                this.SetEndDate(2018, 8, 4);
                this.SetCash(100000);
                var aapl = this.AddEquity("AAPL", Resolution.Hour).Symbol;
                var ibm = this.AddEquity("IBM", Resolution.Hour).Symbol;
                this.AddData(BenzingaNews, aapl);
                this.AddData(BenzingaNews, ibm);
            }
            
            public virtual object OnData(object data) {
                if (this.Time - this.lastTrade < new timedelta(days: 5)) {
                    return;
                }
                // Get rid of our holdings after 5 days, and start fresh
                this.Liquidate();
                // Get all Benzinga data and loop over it
                foreach (var article in data.Get(BenzingaNews).Values) {
                    object selectedSymbol = null;
                    // Use loop instead of list comprehension for clarity purposes
                    // Select the same Symbol we're getting a data point for
                    // from the articles list so that we can get the sentiment of the article
                    // We use the underlying Symbol because the Symbols included in the `Symbols` property
                    // are equity Symbols.
                    foreach (var symbol in article.Symbols) {
                        if (symbol == article.Symbol.Underlying) {
                            selectedSymbol = symbol;
                            break;
                        }
                    }
                    if (selectedSymbol == null) {
                        throw new Exception("Could not find current Symbol {article.Symbol.Underlying} even though it should exist");
                    }
                    // The intersection of the article contents and the pre-defined words are the words that are included in both collections
                    var intersection = new HashSet<object>(article.Contents.lower().split(" ")).intersection(this.words.keys().ToList());
                    // Get the words, then get the aggregate sentiment
                    var sentimentSum = (from i in intersection
                        select this.words[i]).ToList().Sum();
                    if (sentimentSum >= 0.5) {
                        this.Log("Longing {article.Symbol.Underlying} with sentiment score of {sentimentSum}");
                        this.SetHoldings(article.Symbol.Underlying, sentimentSum / 5);
                        this.lastTrade = this.Time;
                    }
                    if (sentimentSum <= -0.5) {
                        this.Log("Shorting {article.Symbol.Underlying} with sentiment score of {sentimentSum}");
                        this.SetHoldings(article.Symbol.Underlying, sentimentSum / 5);
                        this.lastTrade = this.Time;
                    }
                }
            }
            
            public virtual object OnSecuritiesChanged(object changes) {
                foreach (var r in changes.RemovedSecurities) {
                    // If removed from the universe, liquidate and remove the custom data from the algorithm
                    this.Liquidate(r.Symbol);
                    this.RemoveSecurity(Symbol.CreateBase(BenzingaNews, r.Symbol, Market.USA));
                }
            }
        }
    }
}
