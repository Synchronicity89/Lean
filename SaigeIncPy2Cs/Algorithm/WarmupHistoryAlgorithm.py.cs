
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class WarmupHistoryAlgorithm {
    
    static WarmupHistoryAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class WarmupHistoryAlgorithm
        : QCAlgorithm {
        
        public object fast;
        
        public object slow;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2014, 5, 2);
            this.SetEndDate(2014, 5, 2);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            var forex = this.AddForex("EURUSD", Resolution.Second);
            forex = this.AddForex("NZDUSD", Resolution.Second);
            var fast_period = 60;
            var slow_period = 3600;
            this.fast = this.EMA("EURUSD", fast_period);
            this.slow = this.EMA("EURUSD", slow_period);
            // "slow_period + 1" because rolling window waits for one to fall off the back to be considered ready
            // History method returns a dict with a pandas.DataFrame
            var history = this.History(new List<string> {
                "EURUSD",
                "NZDUSD"
            }, slow_period + 1);
            // prints out the tail of the dataframe
            this.Log(history.loc["EURUSD"].tail().ToString());
            this.Log(history.loc["NZDUSD"].tail().ToString());
            foreach (var _tup_1 in history.loc["EURUSD"].iterrows()) {
                var index = _tup_1.Item1;
                var row = _tup_1.Item2;
                this.fast.Update(index, row["close"]);
                this.slow.Update(index, row["close"]);
            }
            this.Log("FAST {0} READY. Samples: {1}".format(this.fast.IsReady ? "IS" : "IS NOT", this.fast.Samples));
            this.Log("SLOW {0} READY. Samples: {1}".format(this.slow.IsReady ? "IS" : "IS NOT", this.slow.Samples));
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (this.fast.Current.Value > this.slow.Current.Value) {
                this.SetHoldings("EURUSD", 1);
            } else {
                this.SetHoldings("EURUSD", -1);
            }
        }
    }
}
