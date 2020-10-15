
using AddReference = clr.AddReference;

public static class WarmupAlgorithm {
    
    static WarmupAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class WarmupAlgorithm
        : QCAlgorithm {
        
        public object fast;
        
        public bool first;
        
        public object slow;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 8);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Second);
            var fast_period = 60;
            var slow_period = 3600;
            this.fast = this.EMA("SPY", fast_period);
            this.slow = this.EMA("SPY", slow_period);
            this.SetWarmup(slow_period);
            this.first = true;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (this.first && !this.IsWarmingUp) {
                this.first = false;
                this.Log("Fast: {0}".format(this.fast.Samples));
                this.Log("Slow: {0}".format(this.slow.Samples));
            }
            if (this.fast.Current.Value > this.slow.Current.Value) {
                this.SetHoldings("SPY", 1);
            } else {
                this.SetHoldings("SPY", -1);
            }
        }
    }
}
