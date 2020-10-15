
using clr;

public static class MovingAverageCrossAlgorithm {
    
    static MovingAverageCrossAlgorithm() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Indicators");
        clr.AddReference("QuantConnect.Common");
    }
    
    public class MovingAverageCrossAlgorithm
        : QCAlgorithm {
        
        public object fast;
        
        public object previous;
        
        public object slow;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2009, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY");
            // create a 15 day exponential moving average
            this.fast = this.EMA("SPY", 15, Resolution.Daily);
            // create a 30 day exponential moving average
            this.slow = this.EMA("SPY", 30, Resolution.Daily);
            this.previous = null;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            // a couple things to notice in this method:
            //  1. We never need to 'update' our indicators with the data, the engine takes care of this for us
            //  2. We can use indicators directly in math expressions
            //  3. We can easily plot many indicators at the same time
            // wait for our slow ema to fully initialize
            if (!this.slow.IsReady) {
                return;
            }
            // only once per day
            if (this.previous != null && this.previous.date() == this.Time.date()) {
                return;
            }
            // define a small tolerance on our checks to avoid bouncing
            var tolerance = 0.00015;
            var holdings = this.Portfolio["SPY"].Quantity;
            // we only want to go long if we're currently short or flat
            if (holdings <= 0) {
                // if the fast is greater than the slow, we'll go long
                if (this.fast.Current.Value > this.slow.Current.Value * (1 + tolerance)) {
                    this.Log("BUY  >> {0}".format(this.Securities["SPY"].Price));
                    this.SetHoldings("SPY", 1.0);
                }
            }
            // we only want to liquidate if we're currently long
            // if the fast is less than the slow we'll liquidate our long
            if (holdings > 0 && this.fast.Current.Value < this.slow.Current.Value) {
                this.Log("SELL >> {0}".format(this.Securities["SPY"].Price));
                this.Liquidate("SPY");
            }
            this.previous = this.Time;
        }
    }
}
