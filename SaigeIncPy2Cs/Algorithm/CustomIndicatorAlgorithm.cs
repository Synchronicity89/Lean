
using AddReference = clr.AddReference;

using deque = collections.deque;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using sum = numpy.sum;

using System.Linq;

public static class CustomIndicatorAlgorithm {
    
    static CustomIndicatorAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomIndicatorAlgorithm
        : QCAlgorithm {
        
        public object custom;
        
        public object customWindow;
        
        public object sma;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.AddEquity("SPY", Resolution.Second);
            // Create a QuantConnect indicator and a python custom indicator for comparison
            this.sma = this.SMA("SPY", 60, Resolution.Minute);
            this.custom = new CustomSimpleMovingAverage("custom", 60);
            // The python custom class must inherit from PythonIndicator to enable Updated event handler
            this.custom.Updated += this.CustomUpdated;
            this.customWindow = RollingWindow[IndicatorDataPoint](5);
            this.RegisterIndicator("SPY", this.custom, Resolution.Minute);
            this.PlotIndicator("cSMA", this.custom);
        }
        
        public virtual object CustomUpdated(object sender, object updated) {
            this.customWindow.Add(updated);
        }
        
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 1);
            }
            if (this.Time.second == 0) {
                this.Log("   sma -> IsReady: {self.sma.IsReady}. Value: {self.sma.Current.Value}");
                this.Log("custom -> IsReady: {self.custom.IsReady}. Value: {self.custom.Value}");
            }
            // Regression test: test fails with an early quit
            var diff = abs(this.custom.Value - this.sma.Current.Value);
            if (diff > 1E-10) {
                this.Quit("Quit: indicators difference is {diff}");
            }
        }
        
        public virtual object OnEndOfAlgorithm() {
            foreach (var item in this.customWindow) {
                this.Log("{item}");
            }
        }
    }
    
    public class CustomSimpleMovingAverage
        : PythonIndicator {
        
        public object Name;
        
        public deque queue;
        
        public int Value;
        
        public CustomSimpleMovingAverage(object name, object period) {
            this.Name = name;
            this.Value = 0;
            this.queue = new deque(maxlen: period);
        }
        
        // Update method is mandatory
        public virtual object Update(object input) {
            this.queue.appendleft(input.Value);
            var count = this.queue.Count;
            this.Value = this.queue.Sum() / count;
            return count == this.queue.maxlen;
        }
    }
}
