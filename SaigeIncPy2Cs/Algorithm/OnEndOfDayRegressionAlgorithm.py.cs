
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class OnEndOfDayRegressionAlgorithm {
    
    static OnEndOfDayRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Test algorithm verifying OnEndOfDay callbacks are called as expected. See GH issue 2865.
    public class OnEndOfDayRegressionAlgorithm
        : QCAlgorithm {
        
        public object _bacSymbol;
        
        public object _ibmSymbol;
        
        public int _onEndOfDayBacCallCount;
        
        public int _onEndOfDayIbmCallCount;
        
        public int _onEndOfDaySpyCallCount;
        
        public object _spySymbol;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            this._spySymbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            this._bacSymbol = Symbol.Create("BAC", SecurityType.Equity, Market.USA);
            this._ibmSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            this._onEndOfDaySpyCallCount = 0;
            this._onEndOfDayBacCallCount = 0;
            this._onEndOfDayIbmCallCount = 0;
            this.AddUniverse("my_universe_name", this.selection);
        }
        
        public virtual object selection(object time) {
            if (time.day == 8) {
                return new List<object> {
                    this._spySymbol.Value,
                    this._ibmSymbol.Value
                };
            }
            return new List<object> {
                this._spySymbol.Value
            };
        }
        
        // We expect it to be called on each day after the first selection process
        //         happens and the algorithm has a security in it
        //         
        public virtual object OnEndOfDay(object symbol) {
            if (symbol == this._spySymbol) {
                if (this._onEndOfDaySpyCallCount == 0) {
                    // just the first time
                    this.SetHoldings(this._spySymbol, 0.5);
                    this.AddEquity("BAC");
                }
                this._onEndOfDaySpyCallCount += 1;
            }
            if (symbol == this._bacSymbol) {
                if (this._onEndOfDayBacCallCount == 0) {
                    // just the first time
                    this.SetHoldings(this._bacSymbol, 0.5);
                }
                this._onEndOfDayBacCallCount += 1;
            }
            if (symbol == this._ibmSymbol) {
                this._onEndOfDayIbmCallCount += 1;
            }
            this.Log("OnEndOfDay() called: " + this.UtcTime.ToString() + ". SPY count " + this._onEndOfDaySpyCallCount.ToString() + ". BAC count " + this._onEndOfDayBacCallCount.ToString() + ". IBM count " + this._onEndOfDayIbmCallCount.ToString());
        }
        
        // Assert expected behavior
        public virtual object OnEndOfAlgorithm() {
            if (this._onEndOfDaySpyCallCount != 5) {
                throw new ValueError("OnEndOfDay(SPY) unexpected count call " + this._onEndOfDaySpyCallCount.ToString());
            }
            if (this._onEndOfDayBacCallCount != 4) {
                throw new ValueError("OnEndOfDay(BAC) unexpected count call " + this._onEndOfDayBacCallCount.ToString());
            }
            if (this._onEndOfDayIbmCallCount != 1) {
                throw new ValueError("OnEndOfDay(IBM) unexpected count call " + this._onEndOfDayIbmCallCount.ToString());
            }
        }
    }
}
