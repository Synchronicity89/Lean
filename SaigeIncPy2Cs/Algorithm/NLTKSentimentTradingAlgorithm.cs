
using clr;

using pd = pandas;

using nltk;

using System.Collections.Generic;

using System.Linq;

public static class NLTKSentimentTradingAlgorithm {
    
    static NLTKSentimentTradingAlgorithm() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Common");
    }
    
    public class NLTKSentimentTradingAlgorithm
        : QCAlgorithm {
        
        public List<object> symbols;
        
        public object text;
        
        public virtual object Initialize() {
            this.SetStartDate(2018, 1, 1);
            this.SetEndDate(2019, 1, 1);
            this.SetCash(100000);
            var spy = this.AddEquity("SPY", Resolution.Minute);
            this.text = this.get_text();
            this.symbols = new List<object> {
                spy.Symbol
            };
            // for what extra models needed to download, please use code nltk.download()
            nltk.download("punkt");
            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", 30), this.Trade);
        }
        
        public virtual object Trade() {
            var current_time = "{self.Time.year}-{self.Time.month}-{self.Time.day}";
            var current_text = this.text.loc[current_time][0];
            var words = nltk.word_tokenize(current_text);
            // users should decide their own positive and negative words
            var positive_word = "Up";
            var negative_word = "Down";
            foreach (var holding in this.Portfolio.Values) {
                // liquidate if it contains negative words
                if (words.Contains(negative_word) && holding.Invested) {
                    this.Liquidate(holding.Symbol);
                }
                // buy if it contains positive words
                if (words.Contains(positive_word) && !holding.Invested) {
                    this.SetHoldings(holding.Symbol, 1 / this.symbols.Count);
                }
            }
        }
        
        public virtual object get_text() {
            // import custom data
            // Note: dl must be 1, or it will not download automatically
            var url = "https://www.dropbox.com/s/7xgvkypg6uxp6xl/EconomicNews.csv?dl=1";
            var data = this.Download(url).split("\n");
            var headline = (from x in data
                select x.split(",")[1]).ToList()[1];
            var date = (from x in data
                select x.split(",")[0]).ToList()[1];
            // create a pd dataframe with 1st col being date and 2nd col being headline (content of the text)
            var df = pd.DataFrame(headline, index: date, columns: new List<string> {
                "headline"
            });
            return df;
        }
    }
}
