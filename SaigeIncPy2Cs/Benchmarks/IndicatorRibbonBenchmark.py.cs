namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    using np = numpy;
    
    using datetime = datetime.datetime;
    
    using System.Collections.Generic;
    
    public static class IndicatorRibbonBenchmark {
        
        static IndicatorRibbonBenchmark() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Indicators");
        }
        
        public class IndicatorRibbonBenchmark
            : QCAlgorithm {
            
            public List<object> ribbon;
            
            public object sma;
            
            public object spy;
            
            // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
            public virtual object Initialize() {
                this.SetStartDate(2010, 1, 1);
                this.SetEndDate(2018, 1, 1);
                this.spy = this.AddEquity("SPY", Resolution.Minute).Symbol;
                var count = 50;
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
            }
            
            public virtual object OnData(object data) {
                // wait for our entire ribbon to be ready
                if (!all(from x in this.ribbon
                    select x.IsReady)) {
                    return;
                }
                foreach (var x in this.ribbon) {
                    var value = x.Current.Value;
                }
            }
        }
    }
}
