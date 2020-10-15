namespace AltData {
    
    using AddReference = clr.AddReference;
    
    using datetime = datetime.datetime;
    
    using timedelta = datetime.timedelta;
    
    public static class USTreasuryYieldCurveRateAlgorithm {
        
        static USTreasuryYieldCurveRateAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class USTreasuryYieldCurveRateAlgorithm
            : QCAlgorithm {
            
            public object lastInversion;
            
            public object spy;
            
            public object yieldCurve;
            
            public virtual object Initialize() {
                this.SetStartDate(2000, 3, 1);
                this.SetEndDate(2019, 9, 15);
                this.SetCash(100000);
                this.spy = this.AddEquity("SPY", Resolution.Hour).Symbol;
                this.yieldCurve = this.AddData(USTreasuryYieldCurveRate, "USTYCR", Resolution.Daily).Symbol;
                this.lastInversion = new datetime(1, 1, 1);
                // Request 60 days of history with the USTreasuryYieldCurveRate custom data Symbol.
                var history = this.History(USTreasuryYieldCurveRate, this.yieldCurve, 60, Resolution.Daily);
                // Count the number of items we get from our history request
                this.Debug("We got {len(history)} items from our history request");
            }
            
            public virtual object OnData(object data) {
                if (!data.ContainsKey(this.yieldCurve)) {
                    return;
                }
                var rates = data[this.yieldCurve];
                // Check for None before using the values
                if (rates.TenYear == null || rates.TwoYear == null) {
                    return;
                }
                // Only advance if a year has gone by
                if (this.Time - this.lastInversion < new timedelta(days: 365)) {
                    return;
                }
                // if there is a yield curve inversion after not having one for a year, short SPY for two years
                if (!this.Portfolio.Invested && rates.TwoYear > rates.TenYear) {
                    this.Debug("{self.Time} - Yield curve inversion! Shorting the market for two years");
                    this.SetHoldings(this.spy, -0.5);
                    this.lastInversion = this.Time;
                    return;
                }
                // If two years have passed, liquidate our position in SPY
                if (this.Time - this.lastInversion >= new timedelta(days: 365 * 2)) {
                    this.Liquidate(this.spy);
                }
            }
        }
    }
}
