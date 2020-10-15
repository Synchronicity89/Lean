
using AddReference = clr.AddReference;

using TradeBar = QuantConnect.Data.Market.TradeBar;

public static class RollingWindowAlgorithm {
    
    static RollingWindowAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class RollingWindowAlgorithm
        : QCAlgorithm {
        
        public object sma;
        
        public object smaWin;
        
        public object window;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 1);
            this.SetEndDate(2013, 11, 1);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Daily);
            // Creates a Rolling Window indicator to keep the 2 TradeBar
            this.window = RollingWindow[TradeBar](2);
            // Creates an indicator and adds to a rolling window when it is updated
            this.sma = this.SMA("SPY", 5);
            this.sma.Updated += this.SmaUpdated;
            this.smaWin = RollingWindow[IndicatorDataPoint](5);
        }
        
        // Adds updated values to rolling window
        public virtual object SmaUpdated(object sender, object updated) {
            this.smaWin.Add(updated);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            // Add SPY TradeBar in rollling window
            this.window.Add(data["SPY"]);
            // Wait for windows to be ready.
            if (!(this.window.IsReady && this.smaWin.IsReady)) {
                return;
            }
            var currBar = this.window[0];
            var pastBar = this.window[1];
            this.Log("Price: {0} -> {1} ... {2} -> {3}".format(pastBar.Time, pastBar.Close, currBar.Time, currBar.Close));
            var currSma = this.smaWin[0];
            var pastSma = this.smaWin[this.smaWin.Count - 1];
            this.Log("SMA:   {0} -> {1} ... {2} -> {3}".format(pastSma.Time, pastSma.Value, currSma.Time, currSma.Value));
            if (!this.Portfolio.Invested && currSma.Value > pastSma.Value) {
                this.SetHoldings("SPY", 1);
            }
        }
    }
}
