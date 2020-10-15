
using AddReference = clr.AddReference;

public static class SmartInsiderDataAlgorithm {
    
    static SmartInsiderDataAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class SmartInsiderDataAlgoritm
        : QCAlgorithm {
        
        public object symbol;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2019, 7, 25);
            this.SetEndDate(2019, 8, 2);
            this.SetCash(100000);
            this.symbol = this.AddEquity("KO", Resolution.Daily).Symbol;
            this.AddData(SmartInsiderTransaction, "KO");
            this.AddData(SmartInsiderIntention, "KO");
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (!data.ContainsKey(this.symbol.Value)) {
                return;
            }
            var has_open_orders = this.Transactions.GetOpenOrders().Count != 0;
            var ko_data = data[this.symbol.Value];
            if (ko_data is SmartInsiderTransaction) {
                if (!this.Portfolio.Invested && !has_open_orders) {
                    if (ko_data.BuybackPercentage > 0.0001 && ko_data.VolumePercentage > 0.001) {
                        this.Log("Buying {self.symbol.Value} due to stock transaction");
                        this.SetHoldings(this.symbol, 0.5);
                    }
                }
            } else if (ko_data is SmartInsiderIntention) {
                if (!this.Portfolio.Invested && !has_open_orders) {
                    if (ko_data.Percentage > 0.0001) {
                        this.Log("Buying {self.symbol.Value} due to intention to purchase stock");
                        this.SetHoldings(this.symbol, 0.5);
                    }
                } else if (this.Portfolio.Invested && !has_open_orders) {
                    if (ko_data.Percentage < 0.0) {
                        this.Log("Liquidating {self.symbol.Value}");
                        this.Liquidate(this.symbol);
                    }
                }
            }
        }
    }
}
