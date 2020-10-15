
using AddReference = clr.AddReference;

using Tick = QuantConnect.Data.Market.Tick;

public static class FilteredIdentityAlgorithm {
    
    static FilteredIdentityAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    //  Example algorithm of the Identity indicator with the filtering enhancement 
    public class FilteredIdentityAlgorithm
        : QCAlgorithm {
        
        public object identity;
        
        public object symbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2014, 5, 2);
            this.SetEndDate(this.StartDate);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            var security = this.AddForex("EURUSD", Resolution.Tick);
            this.symbol = security.Symbol;
            this.identity = this.FilteredIdentity(this.symbol, null, this.Filter);
        }
        
        // Filter function: True if data is not an instance of Tick. If it is, true if TickType is Trade
        //         data -- Data for applying the filter
        public virtual object Filter(object data) {
            if (data is Tick) {
                return data.TickType == TickType.Trade;
            }
            return true;
        }
        
        public virtual object OnData(object data) {
            // Since we are only accepting TickType.Trade,
            // this indicator will never be ready
            if (!this.identity.IsReady) {
                return;
            }
            if (!this.Portfolio.Invested) {
                this.SetHoldings(this.symbol, 1);
                this.Debug("Purchased Stock");
            }
        }
    }
}
