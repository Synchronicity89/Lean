
using AddReference = clr.AddReference;

using ConstituentsUniverse = QuantConnect.Data.UniverseSelection.ConstituentsUniverse;

public static class ConstituentsUniverseRegressionAlgorithm {
    
    static ConstituentsUniverseRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class ConstituentsUniverseRegressionAlgorithm
        : QCAlgorithm {
        
        public object _appl;
        
        public object _fb;
        
        public object _qqq;
        
        public object _spy;
        
        public int _step;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            this._appl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            this._spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            this._qqq = Symbol.Create("QQQ", SecurityType.Equity, Market.USA);
            this._fb = Symbol.Create("FB", SecurityType.Equity, Market.USA);
            this._step = 0;
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.AddUniverse(ConstituentsUniverse(Symbol.Create("constituents-universe-qctest", SecurityType.Equity, Market.USA), this.UniverseSettings));
        }
        
        public virtual object OnData(object data) {
            this._step = this._step + 1;
            if (this._step == 1) {
                if (!data.ContainsKey(this._qqq) || !data.ContainsKey(this._appl)) {
                    throw new ValueError("Unexpected symbols found, step: " + this._step.ToString());
                }
                if (data.Count != 2) {
                    throw new ValueError("Unexpected data count, step: " + this._step.ToString());
                }
                // AAPL will be deselected by the ConstituentsUniverse
                // but it shouldn't be removed since we hold it
                this.SetHoldings(this._appl, 0.5);
            } else if (this._step == 2) {
                if (!data.ContainsKey(this._appl)) {
                    throw new ValueError("Unexpected symbols found, step: " + this._step.ToString());
                }
                if (data.Count != 1) {
                    throw new ValueError("Unexpected data count, step: " + this._step.ToString());
                }
                // AAPL should now be released
                // note: takes one extra loop because the order is executed on market open
                this.Liquidate();
            } else if (this._step == 3) {
                if (!data.ContainsKey(this._fb) || !data.ContainsKey(this._spy) || !data.ContainsKey(this._appl)) {
                    throw new ValueError("Unexpected symbols found, step: " + this._step.ToString());
                }
                if (data.Count != 3) {
                    throw new ValueError("Unexpected data count, step: " + this._step.ToString());
                }
            } else if (this._step == 4) {
                if (!data.ContainsKey(this._fb) || !data.ContainsKey(this._spy)) {
                    throw new ValueError("Unexpected symbols found, step: " + this._step.ToString());
                }
                if (data.Count != 2) {
                    throw new ValueError("Unexpected data count, step: " + this._step.ToString());
                }
            } else if (this._step == 5) {
                if (!data.ContainsKey(this._fb) || !data.ContainsKey(this._spy)) {
                    throw new ValueError("Unexpected symbols found, step: " + this._step.ToString());
                }
                if (data.Count != 2) {
                    throw new ValueError("Unexpected data count, step: " + this._step.ToString());
                }
            }
        }
        
        public virtual object OnEndOfAlgorithm() {
            if (this._step != 5) {
                throw new ValueError("Unexpected step count: " + this._step.ToString());
            }
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            foreach (var added in changes.AddedSecurities) {
                this.Log("AddedSecurities " + added.ToString());
            }
            foreach (var removed in changes.RemovedSecurities) {
                this.Log("RemovedSecurities " + removed.ToString() + this._step.ToString());
                // we are currently notifying the removal of AAPl twice,
                // when deselected and when finally removed (since it stayed pending)
                if (removed.Symbol == this._appl && this._step != 1 && this._step != 2 || removed.Symbol == this._qqq && this._step != 1) {
                    throw new ValueError("Unexpected removal step count: " + this._step.ToString());
                }
            }
        }
    }
}
