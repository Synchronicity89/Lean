
using AddReference = clr.AddReference;

public static class CustomBenchmarkAlgorithm {
    
    static CustomBenchmarkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomBenchmarkAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Second);
            this.SetBenchmark(Symbol.Create("AAPL", SecurityType.Equity, Market.USA));
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 1);
                this.Debug("Purchased Stock");
            }
            var tupleResult = SymbolCache.TryGetSymbol("AAPL", null);
            if (tupleResult[0]) {
                throw new Exception("Benchmark Symbol is not expected to be added to the Symbol cache");
            }
        }
    }
}
