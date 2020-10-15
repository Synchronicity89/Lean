
using AddReference = clr.AddReference;

using List = System.Collections.Generic.List;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using System.Collections.Generic;

using System.Linq;

public static class CoarseFineFundamentalComboAlgorithm {
    
    static CoarseFineFundamentalComboAlgorithm() {
        AddReference("System.Core");
        AddReference("System.Collections");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class CoarseFineFundamentalComboAlgorithm
        : QCAlgorithm {
        
        public int @__numberOfSymbols;
        
        public int @__numberOfSymbolsFine;
        
        public object _changes;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2014, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.SetCash(50000);
            // what resolution should the data *added* to the universe be?
            this.UniverseSettings.Resolution = Resolution.Daily;
            // this add universe method accepts two parameters:
            // - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
            // - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
            this.AddUniverse(this.CoarseSelectionFunction, this.FineSelectionFunction);
            this.@__numberOfSymbols = 5;
            this.@__numberOfSymbolsFine = 2;
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
        
        // sort the data by P/E ratio and take the top 'NumberOfSymbolsFine'
        public virtual object FineSelectionFunction(object fine) {
            // sort descending by P/E ratio
            var sortedByPeRatio = fine.OrderByDescending(x => x.ValuationRatios.PERatio).ToList();
            // take the top entries from our sorted collection
            return (from x in sortedByPeRatio[@::self.__numberOfSymbolsFine]
                select x.Symbol).ToList();
        }
        
        public virtual object OnData(object data) {
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
            // we want 20% allocation in each security in our universe
            foreach (var security in this._changes.AddedSecurities) {
                this.SetHoldings(security.Symbol, 0.2);
            }
            this._changes = null;
        }
        
        // this event fires whenever we have changes to our universe
        public virtual object OnSecuritiesChanged(object changes) {
            this._changes = changes;
        }
    }
}
