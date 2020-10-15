
using AddReference = clr.AddReference;

using np = numpy;

using datetime = datetime.datetime;

using System.Collections.Generic;

using System.Linq;

public static class DisplacedMovingAverageRibbon {
    
    static DisplacedMovingAverageRibbon() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Indicators");
    }
    
    public class DisplacedMovingAverageRibbon
        : QCAlgorithm {
        
        public object previous;
        
        public List<object> ribbon;
        
        public object sma;
        
        public object spy;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2009, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.spy = this.AddEquity("SPY", Resolution.Daily).Symbol;
            var count = 6;
            var offset = 5;
            var period = 15;
            this.ribbon = new List<object>();
            // define our sma as the base of the ribbon
            this.sma = SimpleMovingAverage(period);
            foreach (var x in range(count)) {
                // define our offset to the zero sma, these various offsets will create our 'displaced' ribbon
                var delay = Delay(offset * (x + 1));
                // define an indicator that takes the output of the sma and pipes it into our delay indicator
                var delayedSma = IndicatorExtensions.Of(delay, this.sma);
                // register our new 'delayedSma' for automaic updates on a daily resolution
                this.RegisterIndicator(this.spy, delayedSma, Resolution.Daily);
                this.ribbon.append(delayedSma);
            }
            this.previous = datetime.min;
            // plot indicators each time they update using the PlotIndicator function
            foreach (var i in this.ribbon) {
                this.PlotIndicator("Ribbon", i);
            }
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (data[this.spy] == null) {
                return;
            }
            // wait for our entire ribbon to be ready
            if (!all(from x in this.ribbon
                select x.IsReady)) {
                return;
            }
            // only once per day
            if (this.previous.date() == this.Time.date()) {
                return;
            }
            this.Plot("Ribbon", "Price", data[this.spy].Price);
            // check for a buy signal
            var values = (from x in this.ribbon
                select x.Current.Value).ToList();
            var holding = this.Portfolio[this.spy];
            if (holding.Quantity <= 0 && this.IsAscending(values)) {
                this.SetHoldings(this.spy, 1.0);
            } else if (holding.Quantity > 0 && this.IsDescending(values)) {
                this.Liquidate(this.spy);
            }
            this.previous = this.Time;
        }
        
        // Returns true if the specified values are in ascending order
        public virtual object IsAscending(object values) {
            object last = null;
            foreach (var val in values) {
                if (last == null) {
                    last = val;
                    continue;
                }
                if (last < val) {
                    return false;
                }
                last = val;
            }
            return true;
        }
        
        // Returns true if the specified values are in Descending order
        public virtual object IsDescending(object values) {
            object last = null;
            foreach (var val in values) {
                if (last == null) {
                    last = val;
                    continue;
                }
                if (last > val) {
                    return false;
                }
                last = val;
            }
            return true;
        }
    }
}
