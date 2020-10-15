
using AddReference = clr.AddReference;

using System;

public static class ParameterizedAlgorithm {
    
    static ParameterizedAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class ParameterizedAlgorithm
        : QCAlgorithm {
        
        public object fast;
        
        public object slow;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY");
            // Receive parameters from the Job
            var ema_fast = this.GetParameter("ema-fast");
            var ema_slow = this.GetParameter("ema-slow");
            // The values 100 and 200 are just default values that only used if the parameters do not exist
            var fast_period = ema_fast == null ? 100 : Convert.ToInt32(ema_fast);
            var slow_period = ema_slow == null ? 200 : Convert.ToInt32(ema_slow);
            this.fast = this.EMA("SPY", fast_period);
            this.slow = this.EMA("SPY", slow_period);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            // wait for our indicators to ready
            if (!this.fast.IsReady || !this.slow.IsReady) {
                return;
            }
            var fast = this.fast.Current.Value;
            var slow = this.slow.Current.Value;
            if (fast > slow * 1.001) {
                this.SetHoldings("SPY", 1);
            } else if (fast < slow * 0.999) {
                this.Liquidate("SPY");
            }
        }
    }
}
