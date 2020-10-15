namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    public static class EmptySingleSecuritySecondEquityBenchmark {
        
        static EmptySingleSecuritySecondEquityBenchmark() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Indicators");
            AddReference("QuantConnect.Common");
        }
        
        public class EmptySingleSecuritySecondEquityBenchmark
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2008, 1, 1);
                this.SetEndDate(2009, 1, 1);
                this.SetBenchmark(x => 1);
                this.AddEquity("SPY", Resolution.Second);
            }
            
            public virtual object OnData(object data) {
            }
        }
    }
}
