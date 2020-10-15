
using AddReference = clr.AddReference;

using List = System.Collections.Generic.List;

using System.Collections.Generic;

using System.Linq;

public static class EmaCrossUniverseSelectionAlgorithm {
    
    static EmaCrossUniverseSelectionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class EmaCrossUniverseSelectionAlgorithm
        : QCAlgorithm {
        
        public Dictionary<object, object> averages;
        
        public int coarse_count;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2010, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.SetCash(100000);
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.UniverseSettings.Leverage = 2;
            this.coarse_count = 10;
            this.averages = new Dictionary<object, object> {
            };
            // this add universe method accepts two parameters:
            // - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
            this.AddUniverse(this.CoarseSelectionFunction);
        }
        
        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        public virtual object CoarseSelectionFunction(object coarse) {
            // We are going to use a dictionary to refer the object that will keep the moving averages
            foreach (var cf in coarse) {
                if (!this.averages.Contains(cf.Symbol)) {
                    this.averages[cf.Symbol] = new SymbolData(cf.Symbol);
                }
                // Updates the SymbolData object with current EOD price
                var avg = this.averages[cf.Symbol];
                avg.update(cf.EndTime, cf.AdjustedPrice);
            }
            // Filter the values of the dict: we only want up-trending securities
            var values = this.averages.values().Where(x => x.is_uptrend).ToList().ToList();
            // Sorts the values of the dict: we want those with greater difference between the moving averages
            values.sort(key: x => x.scale, reverse: true);
            foreach (var x in values[::self.coarse_count]) {
                this.Log("symbol: " + x.symbol.Value.ToString() + "  scale: " + x.scale.ToString());
            }
            // we need to return only the symbol objects
            return (from x in values[::self.coarse_count]
                select x.symbol).ToList();
        }
        
        // this event fires whenever we have changes to our universe
        public virtual object OnSecuritiesChanged(object changes) {
            // liquidate removed securities
            foreach (var security in changes.RemovedSecurities) {
                if (security.Invested) {
                    this.Liquidate(security.Symbol);
                }
            }
            // we want 20% allocation in each security in our universe
            foreach (var security in changes.AddedSecurities) {
                this.SetHoldings(security.Symbol, 0.1);
            }
        }
    }
    
    public class SymbolData
        : object {
        
        public object fast;
        
        public bool is_uptrend;
        
        public double scale;
        
        public object slow;
        
        public object symbol;
        
        public double tolerance;
        
        public SymbolData(object symbol) {
            this.symbol = symbol;
            this.tolerance = 1.01;
            this.fast = ExponentialMovingAverage(100);
            this.slow = ExponentialMovingAverage(300);
            this.is_uptrend = false;
            this.scale = 0;
        }
        
        public virtual object update(object time, object value) {
            if (this.fast.Update(time, value) && this.slow.Update(time, value)) {
                var fast = this.fast.Current.Value;
                var slow = this.slow.Current.Value;
                this.is_uptrend = fast > slow * this.tolerance;
            }
            if (this.is_uptrend) {
                this.scale = (fast - slow) / ((fast + slow) / 2.0);
            }
        }
    }
}
