namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    using List = System.Collections.Generic.List;
    
    using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class CoarseFineUniverseSelectionBenchmark {
        
        static CoarseFineUniverseSelectionBenchmark() {
            AddReference("System.Core");
            AddReference("System.Collections");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
        }
        
        public class CoarseFineUniverseSelectionBenchmark
            : QCAlgorithm {
            
            public object _changes;
            
            public int numberOfSymbols;
            
            public int numberOfSymbolsFine;
            
            public virtual object Initialize() {
                this.SetStartDate(2017, 11, 1);
                this.SetEndDate(2018, 1, 1);
                this.SetCash(50000);
                this.UniverseSettings.Resolution = Resolution.Minute;
                this.AddUniverse(this.CoarseSelectionFunction, this.FineSelectionFunction);
                this.numberOfSymbols = 150;
                this.numberOfSymbolsFine = 40;
                this._changes = null;
            }
            
            // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
            public virtual object CoarseSelectionFunction(object coarse) {
                var selected = (from x in coarse
                    where x.HasFundamentalData
                    select x).ToList();
                // sort descending by daily dollar volume
                var sortedByDollarVolume = selected.OrderByDescending(x => x.DollarVolume).ToList();
                // return the symbol objects of the top entries from our sorted collection
                return (from x in sortedByDollarVolume[::self.numberOfSymbols]
                    select x.Symbol).ToList();
            }
            
            // sort the data by P/E ratio and take the top 'NumberOfSymbolsFine'
            public virtual object FineSelectionFunction(object fine) {
                // sort descending by P/E ratio
                var sortedByPeRatio = fine.OrderByDescending(x => x.ValuationRatios.PERatio).ToList();
                // take the top entries from our sorted collection
                return (from x in sortedByPeRatio[::self.numberOfSymbolsFine]
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
                foreach (var security in this._changes.AddedSecurities) {
                    this.SetHoldings(security.Symbol, 0.02);
                }
                this._changes = null;
            }
            
            public virtual object OnSecuritiesChanged(object changes) {
                this._changes = changes;
            }
        }
    }
}
