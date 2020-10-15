
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using System.Collections.Generic;

using System.Linq;

public static class CoarseFundamentalTop3Algorithm {
    
    static CoarseFundamentalTop3Algorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class CoarseFundamentalTop3Algorithm
        : QCAlgorithm {
        
        public int @__numberOfSymbols;
        
        public object _changes;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2014, 3, 24);
            this.SetEndDate(2014, 4, 7);
            this.SetCash(50000);
            // what resolution should the data *added* to the universe be?
            this.UniverseSettings.Resolution = Resolution.Daily;
            // this add universe method accepts a single parameter that is a function that
            // accepts an IEnumerable<CoarseFundamental> and returns IEnumerable<Symbol>
            this.AddUniverse(this.CoarseSelectionFunction);
            this.@__numberOfSymbols = 3;
            this._changes = null;
        }
        
        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        public virtual object CoarseSelectionFunction(object coarse) {
            // sort descending by daily dollar volume
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume).ToList();
            // return the symbol objects of the top entries from our sorted collection
            return (from x in sortedByDollarVolume[@::self.__numberOfSymbols]
                select x.Symbol).ToList();
        }
        
        public virtual object OnData(object data) {
            this.Log("OnData({self.UtcTime}): Keys: {', '.join([key.Value for key in data.Keys])}");
            // if we have no changes, do nothing
            if (this._changes == null) {
                return;
            }
            // liquidate removed securities
            foreach (var security in this._changes.RemovedSecurities) {
                if (security.Invested) {
                    this.Liquidate(security.Symbol);
                }
            }
            // we want 1/N allocation in each security in our universe
            foreach (var security in this._changes.AddedSecurities) {
                this.SetHoldings(security.Symbol, 1 / this.@__numberOfSymbols);
            }
            this._changes = null;
        }
        
        // this event fires whenever we have changes to our universe
        public virtual object OnSecuritiesChanged(object changes) {
            this._changes = changes;
            this.Log("OnSecuritiesChanged({self.UtcTime}):: {changes}");
        }
        
        public virtual object OnOrderEvent(object fill) {
            this.Log("OnOrderEvent({self.UtcTime}):: {fill}");
        }
    }
}
