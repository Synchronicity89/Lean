
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

public static class MarketOnOpenOnCloseAlgorithm {
    
    static MarketOnOpenOnCloseAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class MarketOnOpenOnCloseAlgorithm
        : QCAlgorithm {
        
        public object @__last;
        
        public bool @__submittedMarketOnCloseToday;
        
        public object equity;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.equity = this.AddEquity("SPY", Resolution.Second, fillDataForward: true, extendedMarketHours: true);
            this.@__submittedMarketOnCloseToday = false;
            this.@__last = datetime.min;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (this.Time.date() != this.@__last.date()) {
                // each morning submit a market on open order
                this.@__submittedMarketOnCloseToday = false;
                this.MarketOnOpenOrder("SPY", 100);
                this.@__last = this.Time;
            }
            if (!this.@__submittedMarketOnCloseToday && this.equity.Exchange.ExchangeOpen) {
                // once the exchange opens submit a market on close order
                this.@__submittedMarketOnCloseToday = true;
                this.MarketOnCloseOrder("SPY", -100);
            }
        }
        
        public virtual object OnOrderEvent(object fill) {
            var order = this.Transactions.GetOrderById(fill.OrderId);
            this.Log("{0} - {1}:: {2}".format(this.Time, order.Type, fill));
        }
    }
}
