
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class BasicTemplateFuturesConsolidationAlgorithm {
    
    static BasicTemplateFuturesConsolidationAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateFuturesConsolidationAlgorithm
        : QCAlgorithm {
        
        public dict consolidators;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(1000000);
            // Subscribe and set our expiry filter for the futures chain
            var future = this.AddFuture(Futures.Indices.SP500EMini);
            future.SetFilter(new timedelta(0), new timedelta(182));
            this.consolidators = new dict();
        }
        
        public virtual object OnData(object slice) {
        }
        
        public virtual object OnDataConsolidated(object sender, object quoteBar) {
            this.Log("OnDataConsolidated called on " + this.Time.ToString());
            this.Log(quoteBar.ToString());
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            object consolidator;
            foreach (var security in changes.AddedSecurities) {
                consolidator = QuoteBarConsolidator(new timedelta(minutes: 5));
                consolidator.DataConsolidated += this.OnDataConsolidated;
                this.SubscriptionManager.AddConsolidator(security.Symbol, consolidator);
                this.consolidators[security.Symbol] = consolidator;
            }
            foreach (var security in changes.RemovedSecurities) {
                consolidator = this.consolidators.pop(security.Symbol);
                this.SubscriptionManager.RemoveConsolidator(security.Symbol, consolidator);
                consolidator.DataConsolidated -= this.OnDataConsolidated;
            }
        }
    }
}
