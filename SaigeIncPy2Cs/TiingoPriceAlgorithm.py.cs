
using AddReference = clr.AddReference;

public static class TiingoPriceAlgorithm {
    
    static TiingoPriceAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class TiingoPriceAlgorithm
        : QCAlgorithm {
        
        public object emaFast;
        
        public object emaSlow;
        
        public object symbol;
        
        public string ticker;
        
        public virtual object Initialize() {
            // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
            this.SetStartDate(2017, 1, 1);
            this.SetEndDate(2017, 12, 31);
            this.SetCash(100000);
            // Set your Tiingo API Token here
            Tiingo.SetAuthCode("my-tiingo-api-token");
            this.ticker = "AAPL";
            this.symbol = this.AddData(TiingoPrice, this.ticker, Resolution.Daily).Symbol;
            this.emaFast = this.EMA(this.symbol, 5);
            this.emaSlow = this.EMA(this.symbol, 10);
        }
        
        public virtual object OnData(object slice) {
            // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            if (!slice.ContainsKey(this.ticker)) {
                return;
            }
            // Extract Tiingo data from the slice
            var row = slice[this.ticker];
            this.Log("{self.Time} - {row.Symbol.Value} - {row.Close} {row.Value} {row.Price} - EmaFast:{self.emaFast} - EmaSlow:{self.emaSlow}");
            // Simple EMA cross
            if (!this.Portfolio.Invested && this.emaFast > this.emaSlow) {
                this.SetHoldings(this.symbol, 1);
            } else if (this.Portfolio.Invested && this.emaFast < this.emaSlow) {
                this.Liquidate(this.symbol);
            }
        }
    }
}
