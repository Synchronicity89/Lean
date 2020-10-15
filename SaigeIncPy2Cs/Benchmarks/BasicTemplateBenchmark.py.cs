namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    public static class BasicTemplateBenchmark {
        
        static BasicTemplateBenchmark() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Indicators");
            AddReference("QuantConnect.Common");
        }
        
        public class BasicTemplateBenchmark
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2000, 1, 1);
                this.SetEndDate(2017, 1, 1);
                this.SetBenchmark(x => 1);
                this.AddEquity("SPY");
            }
            
            public virtual object OnData(object data) {
                if (!this.Portfolio.Invested) {
                    this.SetHoldings("SPY", 1);
                    this.Debug("Purchased Stock");
                }
            }
        }
    }
}
