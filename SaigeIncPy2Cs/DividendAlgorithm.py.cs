
using AddReference = clr.AddReference;

public static class DividendAlgorithm {
    
    static DividendAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class DividendAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(1998, 1, 1);
            this.SetEndDate(2006, 1, 21);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            var equity = this.AddEquity("MSFT", Resolution.Daily);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            // this will use the Tradier Brokerage open order split behavior
            // forward split will modify open order to maintain order value
            // reverse split open orders will be cancelled
            this.SetBrokerageModel(BrokerageName.TradierBrokerage);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            var bar = data["MSFT"];
            if (this.Transactions.OrdersCount == 0) {
                this.SetHoldings("MSFT", 0.5);
                // place some orders that won't fill, when the split comes in they'll get modified to reflect the split
                var quantity = this.CalculateOrderQuantity("MSFT", 0.25);
                this.Debug("Purchased Stock: {bar.Price}");
                this.StopMarketOrder("MSFT", -quantity, bar.Low / 2);
                this.LimitOrder("MSFT", -quantity, bar.High * 2);
            }
            if (data.Dividends.ContainsKey("MSFT")) {
                var dividend = data.Dividends["MSFT"];
                this.Log("{self.Time} >> DIVIDEND >> {dividend.Symbol} - {dividend.Distribution} - {self.Portfolio.Cash} - {self.Portfolio['MSFT'].Price}");
            }
            if (data.Splits.ContainsKey("MSFT")) {
                var split = data.Splits["MSFT"];
                this.Log("{self.Time} >> SPLIT >> {split.Symbol} - {split.SplitFactor} - {self.Portfolio.Cash} - {self.Portfolio['MSFT'].Price}");
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            // orders get adjusted based on split events to maintain order value
            var order = this.Transactions.GetOrderById(orderEvent.OrderId);
            this.Log("{self.Time} >> ORDER >> {order}");
        }
    }
}
