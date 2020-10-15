
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

public static class MACDTrendAlgorithm {
    
    static MACDTrendAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class MACDTrendAlgorithm
        : QCAlgorithm {
        
        public object @__macd;
        
        public object @__previous;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2004, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Daily);
            // define our daily macd(12,26) with a 9 day signal
            this.@__macd = this.MACD("SPY", 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily);
            this.@__previous = datetime.min;
            this.PlotIndicator("MACD", true, this.@__macd, this.@__macd.Signal);
            this.PlotIndicator("SPY", this.@__macd.Fast, this.@__macd.Slow);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            // wait for our macd to fully initialize
            if (!this.@__macd.IsReady) {
                return;
            }
            // only once per day
            if (this.@__previous.date() == this.Time.date()) {
                return;
            }
            // define a small tolerance on our checks to avoid bouncing
            var tolerance = 0.0025;
            var holdings = this.Portfolio["SPY"].Quantity;
            var signalDeltaPercent = (this.@__macd.Current.Value - this.@__macd.Signal.Current.Value) / this.@__macd.Fast.Current.Value;
            // if our macd is greater than our signal, then let's go long
            if (holdings <= 0 && signalDeltaPercent > tolerance) {
                // 0.01%
                // longterm says buy as well
                this.SetHoldings("SPY", 1.0);
            } else if (holdings >= 0 && signalDeltaPercent < -tolerance) {
                // of our macd is less than our signal, then let's go short
                this.Liquidate("SPY");
            }
            this.@__previous = this.Time;
        }
    }
}
