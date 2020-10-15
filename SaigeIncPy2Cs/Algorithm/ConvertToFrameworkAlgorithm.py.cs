
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class ConvertToFrameworkAlgorithm {
    
    static ConvertToFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Indicators");
    }
    
    // Demonstration algorithm showing how to easily convert an old algorithm into the framework.
    public class ConvertToFrameworkAlgorithm
        : QCAlgorithm {
        
        public int FastEmaPeriod;
        
        public object macd;
        
        public int SlowEmaPeriod;
        
        public object symbol;
        
        public int FastEmaPeriod = 12;
        
        public int SlowEmaPeriod = 26;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2004, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.symbol = this.AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily).Symbol;
            // define our daily macd(12,26) with a 9 day signal
            this.macd = this.MACD(this.symbol, this.FastEmaPeriod, this.SlowEmaPeriod, 9, MovingAverageType.Exponential, Resolution.Daily);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        //         Args:
        //             data: Slice object with your stock data
        public virtual object OnData(object data) {
            // wait for our indicator to be ready
            if (!this.macd.IsReady || data[this.symbol] == null) {
                return;
            }
            var holding = this.Portfolio[this.symbol];
            var signalDeltaPercent = float(this.macd.Current.Value - this.macd.Signal.Current.Value) / float(this.macd.Fast.Current.Value);
            var tolerance = 0.0025;
            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity <= 0 && signalDeltaPercent > tolerance) {
                // 1. Call EmitInsights with insights created in correct direction, here we're going long
                //    The EmitInsights method can accept multiple insights separated by commas
                this.EmitInsights(Insight.Price(this.symbol, new timedelta(this.FastEmaPeriod), InsightDirection.Up));
                // longterm says buy as well
                this.SetHoldings(this.symbol, 1);
            } else if (holding.Quantity >= 0 && signalDeltaPercent < -tolerance) {
                // if our macd is less than our signal, then let's go short
                // 1. Call EmitInsights with insights created in correct direction, here we're going short
                //    The EmitInsights method can accept multiple insights separated by commas
                this.EmitInsights(Insight.Price(this.symbol, new timedelta(this.FastEmaPeriod), InsightDirection.Down));
                this.SetHoldings(this.symbol, -1);
            }
            // if we wanted to liquidate our positions
            //# 1. Call EmitInsights with insights create in the correct direction -- Flat
            //self.EmitInsights(
            // Creates an insight for our symbol, predicting that it will move down or up within the fast ema period number of days, depending on our current position
            // Insight.Price(self.symbol, timedelta(self.FastEmaPeriod), InsightDirection.Flat)
            //)
            // self.Liquidate()
            // plot both lines
            this.Plot("MACD", this.macd, this.macd.Signal);
            this.Plot(this.symbol.Value, this.macd.Fast, this.macd.Slow);
            this.Plot(this.symbol.Value, "Open", data[this.symbol].Open);
        }
    }
}
