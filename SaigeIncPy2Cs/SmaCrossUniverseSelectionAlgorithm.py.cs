
using AddReference = clr.AddReference;

using System.Collections.Generic;

using System.Linq;

public static class SmaCrossUniverseSelectionAlgorithm {
    
    static SmaCrossUniverseSelectionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Indicators");
    }
    
    // Provides an example where WarmUpIndicator method is used to warm up indicators
    //     after their security is added and before (Universe Selection scenario)
    public class SmaCrossUniverseSelectionAlgorithm
        : QCAlgorithm {
        
        public dict averages;
        
        public int count;
        
        public bool EnableAutomaticIndicatorWarmUp;
        
        public int targetPercent;
        
        public double tolerance;
        
        public int count = 10;
        
        public double tolerance = 0.01;
        
        public int targetPercent = 1 / count;
        
        public dict averages = new dict();
        
        public virtual object Initialize() {
            this.UniverseSettings.Leverage = 2;
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2018, 1, 1);
            this.SetEndDate(2019, 1, 1);
            this.SetCash(1000000);
            this.EnableAutomaticIndicatorWarmUp = true;
            var ibm = this.AddEquity("IBM", Resolution.Tick).Symbol;
            var ibmSma = this.SMA(ibm, 40);
            this.Log("{ibmSma.Name}: {ibmSma.Current.Time} - {ibmSma}. IsReady? {ibmSma.IsReady}");
            var spy = this.AddEquity("SPY", Resolution.Hour).Symbol;
            var spySma = this.SMA(spy, 10);
            var spyAtr = this.ATR(spy, 10);
            var spyVwap = this.VWAP(spy, 10);
            this.Log("SPY    - Is ready? SMA: {spySma.IsReady}, ATR: {spyAtr.IsReady}, VWAP: {spyVwap.IsReady}");
            var eur = this.AddForex("EURUSD", Resolution.Hour).Symbol;
            var eurSma = this.SMA(eur, 20, Resolution.Daily);
            var eurAtr = this.ATR(eur, 20, MovingAverageType.Simple, Resolution.Daily);
            this.Log("EURUSD - Is ready? SMA: {eurSma.IsReady}, ATR: {eurAtr.IsReady}");
            this.AddUniverse(this.CoarseSmaSelector);
            // Since the indicators are ready, we will receive error messages
            // reporting that the algorithm manager is trying to add old information
            this.SetWarmUp(10);
        }
        
        public virtual object CoarseSmaSelector(object coarse) {
            var score = new dict();
            foreach (var cf in coarse) {
                if (!cf.HasFundamentalData) {
                    continue;
                }
                var symbol = cf.Symbol;
                var price = cf.AdjustedPrice;
                // grab the SMA instance for this symbol
                var avg = this.averages.setdefault(symbol, this.WarmUpIndicator(symbol, SimpleMovingAverage(100), Resolution.Daily));
                // Update returns true when the indicators are ready, so don't accept until they are
                if (avg.Update(cf.EndTime, price)) {
                    var value = avg.Current.Value;
                    // only pick symbols who have their price over their 100 day sma
                    if (value > price * this.tolerance) {
                        score[symbol] = (value - price) / ((value + price) / 2);
                    }
                }
            }
            // prefer symbols with a larger delta by percentage between the two averages
            var sortedScore = score.items().OrderByDescending(kvp => kvp[1]).ToList();
            return (from x in sortedScore[::self.count]
                select x[0]).ToList();
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            foreach (var security in changes.RemovedSecurities) {
                if (security.Invested) {
                    this.Liquidate(security.Symbol);
                }
            }
            foreach (var security in changes.AddedSecurities) {
                this.SetHoldings(security.Symbol, this.targetPercent);
            }
        }
    }
}
