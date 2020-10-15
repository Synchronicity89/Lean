
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

public static class RegressionAlgorithm {
    
    static RegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class RegressionAlgorithm
        : QCAlgorithm {
        
        public object @__lastTradeTicks;
        
        public object @__lastTradeTradeBars;
        
        public timedelta @__tradeEvery;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(10000000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Tick);
            this.AddEquity("BAC", Resolution.Minute);
            this.AddEquity("AIG", Resolution.Hour);
            this.AddEquity("IBM", Resolution.Daily);
            this.@__lastTradeTicks = this.StartDate;
            this.@__lastTradeTradeBars = this.@__lastTradeTicks;
            this.@__tradeEvery = new timedelta(minutes: 1);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (this.Time - this.@__lastTradeTradeBars < this.@__tradeEvery) {
                return;
            }
            this.@__lastTradeTradeBars = this.Time;
            foreach (var kvp in data.Bars) {
                var period = kvp.Value.Period.total_seconds();
                if (this.roundTime(this.Time, period) != this.Time) {
                }
                var symbol = kvp.Key;
                var holdings = this.Portfolio[symbol];
                if (!holdings.Invested) {
                    this.MarketOrder(symbol, 10);
                } else {
                    this.MarketOrder(symbol, -holdings.Quantity);
                }
            }
        }
        
        // Round a datetime object to any time laps in seconds
        //         dt : datetime object, default now.
        //         roundTo : Closest number of seconds to round to, default 1 minute.
        //         
        public virtual object roundTime(object dt = null, object roundTo = 60) {
            if (dt == null) {
                dt = datetime.now();
            }
            var seconds = (dt - dt.min).seconds;
            // // is a floor division, not a comment on following line:
            var rounding = (seconds + roundTo / 2) / roundTo * roundTo;
            return dt + new timedelta(0, (rounding - seconds), -dt.microsecond);
        }
    }
}
