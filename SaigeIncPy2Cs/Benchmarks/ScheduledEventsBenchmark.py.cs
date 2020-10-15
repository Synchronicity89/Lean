namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    using timedelta = datetime.timedelta;
    
    public static class ScheduledEventsBenchmark {
        
        static ScheduledEventsBenchmark() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class ScheduledEventsBenchmark
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2011, 1, 1);
                this.SetEndDate(2018, 1, 1);
                this.SetCash(100000);
                this.AddEquity("SPY");
                foreach (var i in range(300)) {
                    this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", i), this.Rebalance);
                    this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.BeforeMarketClose("SPY", i), this.Rebalance);
                }
            }
            
            public virtual object OnData(object data) {
            }
            
            public virtual object Rebalance() {
            }
        }
    }
}
