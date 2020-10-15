
using AddReference = clr.AddReference;

public static class DelistingEventsAlgorithm {
    
    static DelistingEventsAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class DelistingEventsAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2007, 5, 16);
            this.SetEndDate(2007, 5, 25);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("AAA", Resolution.Daily);
            this.AddEquity("SPY", Resolution.Daily);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            object value;
            object symbol;
            if (this.Transactions.OrdersCount == 0) {
                this.SetHoldings("AAA", 1);
                this.Debug("Purchased stock");
            }
            foreach (var kvp in data.Bars) {
                symbol = kvp.Key;
                value = kvp.Value;
                this.Log("OnData(Slice): {0}: {1}: {2}".format(this.Time, symbol, value.Close));
            }
            // the slice can also contain delisting data: data.Delistings in a dictionary string->Delisting
            var aaa = this.Securities["AAA"];
            if (aaa.IsDelisted && aaa.IsTradable) {
                throw new Exception("Delisted security must NOT be tradable");
            }
            if (!aaa.IsDelisted && !aaa.IsTradable) {
                throw new Exception("Securities must be marked as tradable until they're delisted or removed from the universe");
            }
            foreach (var kvp in data.Delistings) {
                symbol = kvp.Key;
                value = kvp.Value;
                if (value.Type == DelistingType.Warning) {
                    this.Log("OnData(Delistings): {0}: {1} will be delisted at end of day today.".format(this.Time, symbol));
                    // liquidate on delisting warning
                    this.SetHoldings(symbol, 0);
                }
                if (value.Type == DelistingType.Delisted) {
                    this.Log("OnData(Delistings): {0}: {1} has been delisted.".format(this.Time, symbol));
                    // fails because the security has already been delisted and is no longer tradable
                    this.SetHoldings(symbol, 1);
                }
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log("OnOrderEvent(OrderEvent): {0}: {1}".format(this.Time, orderEvent));
        }
    }
}
