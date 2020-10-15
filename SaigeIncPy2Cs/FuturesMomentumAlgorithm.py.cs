
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using np = numpy;

using System.Collections.Generic;

using System.Linq;

public static class FuturesMomentumAlgorithm {
    
    static FuturesMomentumAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class FuturesMomentumAlgorithm
        : QCAlgorithm {
        
        public object _fast;
        
        public object _slow;
        
        public double _tolerance;
        
        public bool IsDownTrend;
        
        public bool IsUpTrend;
        
        public virtual object Initialize() {
            this.SetStartDate(2016, 1, 1);
            this.SetEndDate(2016, 8, 18);
            this.SetCash(100000);
            var fastPeriod = 20;
            var slowPeriod = 60;
            this._tolerance = 1 + 0.001;
            this.IsUpTrend = false;
            this.IsDownTrend = false;
            this.SetWarmUp(max(fastPeriod, slowPeriod));
            // Adds SPY to be used in our EMA indicators
            var equity = this.AddEquity("SPY", Resolution.Daily);
            this._fast = this.EMA(equity.Symbol, fastPeriod, Resolution.Daily);
            this._slow = this.EMA(equity.Symbol, slowPeriod, Resolution.Daily);
            // Adds the future that will be traded and
            // set our expiry filter for this futures chain
            var future = this.AddFuture(Futures.Indices.SP500EMini);
            future.SetFilter(new timedelta(0), new timedelta(182));
        }
        
        public virtual object OnData(object slice) {
            if (this._slow.IsReady && this._fast.IsReady) {
                this.IsUpTrend = this._fast.Current.Value > this._slow.Current.Value * this._tolerance;
                this.IsDownTrend = this._fast.Current.Value < this._slow.Current.Value * this._tolerance;
                if (!this.Portfolio.Invested && this.IsUpTrend) {
                    foreach (var chain in slice.FuturesChains) {
                        // find the front contract expiring no earlier than in 90 days
                        var contracts = chain.Value.Where(x => x.Expiry > this.Time + timedelta(90)).ToList().ToList();
                        // if there is any contract, trade the front contract
                        if (contracts.Count == 0) {
                            continue;
                        }
                        var contract = contracts.OrderByDescending(x => x.Expiry).ToList()[0];
                        this.MarketOrder(contract.Symbol, 1);
                    }
                }
                if (this.Portfolio.Invested && this.IsDownTrend) {
                    this.Liquidate();
                }
            }
        }
        
        public virtual object OnEndOfDay() {
            if (this.IsUpTrend) {
                this.Plot("Indicator Signal", "EOD", 1);
            } else if (this.IsDownTrend) {
                this.Plot("Indicator Signal", "EOD", -1);
            } else if (this._slow.IsReady && this._fast.IsReady) {
                this.Plot("Indicator Signal", "EOD", 0);
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
