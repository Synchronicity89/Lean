
using AddReference = clr.AddReference;

using System;

public static class DailyAlgorithm {
    
    static DailyAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class DailyAlgorithm
        : QCAlgorithm {
        
        public object ema;
        
        public object lastAction;
        
        public object macd;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2014, 1, 1);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Daily);
            this.AddEquity("IBM", Resolution.Hour).SetLeverage(1.0);
            this.macd = this.MACD("SPY", 12, 26, 9, MovingAverageType.Wilders, Resolution.Daily, Field.Close);
            this.ema = this.EMA("IBM", 15 * 6, Resolution.Hour, Field.SevenBar);
            this.lastAction = null;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.macd.IsReady) {
                return;
            }
            if (!data.ContainsKey("IBM")) {
                return;
            }
            if (data["IBM"] == null) {
                this.Log(String.Format("Price Missing Time: %s", this.Time.ToString()));
                return;
            }
            if (this.lastAction != null && this.lastAction.date() == this.Time.date()) {
                return;
            }
            this.lastAction = this.Time;
            var quantity = this.Portfolio["SPY"].Quantity;
            if (quantity <= 0 && this.macd.Current.Value > this.macd.Signal.Current.Value && data["IBM"].Price > this.ema.Current.Value) {
                this.SetHoldings("IBM", 0.25);
            } else if (quantity >= 0 && this.macd.Current.Value < this.macd.Signal.Current.Value && data["IBM"].Price < this.ema.Current.Value) {
                this.SetHoldings("IBM", -0.25);
            }
        }
    }
}
