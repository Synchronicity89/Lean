
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using np = numpy;

using System.Linq;

public static class CustomVolatilityModelAlgorithm {
    
    static CustomVolatilityModelAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomVolatilityModelAlgorithm
        : QCAlgorithm {
        
        public object equity;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2015, 7, 15);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.equity = this.AddEquity("SPY", Resolution.Daily);
            this.equity.SetVolatilityModel(new CustomVolatilityModel(10));
        }
        
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested && this.equity.VolatilityModel.Volatility > 0) {
                this.SetHoldings("SPY", 1);
            }
        }
    }
    
    public class CustomVolatilityModel {
        
        public object lastPrice;
        
        public object lastUpdate;
        
        public bool needsUpdate;
        
        public timedelta periodSpan;
        
        public object Volatility;
        
        public object window;
        
        public CustomVolatilityModel(object periods) {
            this.lastUpdate = datetime.min;
            this.lastPrice = 0;
            this.needsUpdate = false;
            this.periodSpan = new timedelta(1);
            this.window = RollingWindow[float](periods);
            // Volatility is a mandatory attribute
            this.Volatility = 0;
        }
        
        // Updates this model using the new price information in the specified security instance
        // Update is a mandatory method
        public virtual object Update(object security, object data) {
            var timeSinceLastUpdate = data.EndTime - this.lastUpdate;
            if (timeSinceLastUpdate >= this.periodSpan && data.Price > 0) {
                if (this.lastPrice > 0) {
                    this.window.Add(float(data.Price / this.lastPrice) - 1.0);
                    this.needsUpdate = this.window.IsReady;
                }
                this.lastUpdate = data.EndTime;
                this.lastPrice = data.Price;
            }
            if (this.window.Count < 2) {
                this.Volatility = 0;
                return;
            }
            if (this.needsUpdate) {
                this.needsUpdate = false;
                var std = np.std((from x in this.window
                    select x).ToList());
                this.Volatility = std * np.sqrt(252.0);
            }
        }
        
        // Returns history requirements for the volatility model expressed in the form of history request
        // GetHistoryRequirements is a mandatory method
        public virtual object GetHistoryRequirements(object security, object utcTime) {
            // For simplicity's sake, we will not set a history requirement 
            return null;
        }
    }
}
