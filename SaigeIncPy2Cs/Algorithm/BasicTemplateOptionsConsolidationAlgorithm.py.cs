
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class BasicTemplateOptionsConsolidationAlgorithm {
    
    static BasicTemplateOptionsConsolidationAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateOptionsConsolidationAlgorithm
        : QCAlgorithm {
        
        public dict consolidators;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(1000000);
            // Subscribe and set our filter for the options chain
            var option = this.AddOption("SPY");
            option.SetFilter(-2, 2, new timedelta(0), new timedelta(180));
            this.consolidators = new dict();
        }
        
        public virtual object OnData(object slice) {
        }
        
        public virtual object OnQuoteBarConsolidated(object sender, object quoteBar) {
            this.Log("OnQuoteBarConsolidated called on " + this.Time.ToString());
            this.Log(quoteBar.ToString());
        }
        
        public virtual object OnTradeBarConsolidated(object sender, object tradeBar) {
            this.Log("OnTradeBarConsolidated called on " + this.Time.ToString());
            this.Log(tradeBar.ToString());
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            object consolidator;
            foreach (var security in changes.AddedSecurities) {
                if (security.Type == SecurityType.Equity) {
                    consolidator = TradeBarConsolidator(new timedelta(minutes: 5));
                    consolidator.DataConsolidated += this.OnTradeBarConsolidated;
                } else {
                    consolidator = QuoteBarConsolidator(new timedelta(minutes: 5));
                    consolidator.DataConsolidated += this.OnQuoteBarConsolidated;
                }
                this.SubscriptionManager.AddConsolidator(security.Symbol, consolidator);
                this.consolidators[security.Symbol] = consolidator;
            }
            foreach (var security in changes.RemovedSecurities) {
                consolidator = this.consolidators.pop(security.Symbol);
                this.SubscriptionManager.RemoveConsolidator(security.Symbol, consolidator);
                if (security.Type == SecurityType.Equity) {
                    consolidator.DataConsolidated -= this.OnTradeBarConsolidated;
                } else {
                    consolidator.DataConsolidated -= this.OnQuoteBarConsolidated;
                }
            }
        }
    }
}
