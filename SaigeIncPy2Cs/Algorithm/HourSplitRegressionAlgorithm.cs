
using AddReference = clr.AddReference;

public static class HourSplitRegressionAlgorithm {
    
    static HourSplitRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class HourSplitRegressionAlgorithm
        : QCAlgorithm {
        
        public object symbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2005, 2, 25);
            this.SetEndDate(2005, 2, 28);
            this.SetCash(100000);
            this.SetBenchmark(x => 0);
            this.symbol = this.AddEquity("AAPL", Resolution.Hour).Symbol;
        }
        
        public virtual object OnData(object slice) {
            if (slice.Bars.Count == 0) {
                return;
            }
            if (!this.Portfolio.Invested && this.Time.date() == this.EndDate.date()) {
                this.Buy(this.symbol, 1);
            }
        }
    }
}
