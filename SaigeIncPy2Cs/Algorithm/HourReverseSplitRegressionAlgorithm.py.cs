
using AddReference = clr.AddReference;

public static class HourReverseSplitRegressionAlgorithm {
    
    static HourReverseSplitRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class HourReverseSplitRegressionAlgorithm
        : QCAlgorithm {
        
        public object symbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 11, 7);
            this.SetEndDate(2013, 11, 8);
            this.SetCash(100000);
            this.SetBenchmark(x => 0);
            this.symbol = this.AddEquity("VXX", Resolution.Hour).Symbol;
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
