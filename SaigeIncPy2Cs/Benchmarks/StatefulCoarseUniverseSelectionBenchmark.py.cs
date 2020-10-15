namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class StatefulCoarseUniverseSelectionBenchmark {
        
        static StatefulCoarseUniverseSelectionBenchmark() {
            AddReference("System.Core");
            AddReference("System.Collections");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
        }
        
        public class StatefulCoarseUniverseSelectionBenchmark
            : QCAlgorithm {
            
            public List<object> _blackList;
            
            public int numberOfSymbols;
            
            public virtual object Initialize() {
                this.UniverseSettings.Resolution = Resolution.Daily;
                this.SetStartDate(2017, 11, 1);
                this.SetEndDate(2018, 1, 1);
                this.SetCash(50000);
                this.AddUniverse(this.CoarseSelectionFunction);
                this.numberOfSymbols = 250;
                this._blackList = new List<object>();
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
                    where !this._blackList.Contains(x.Symbol)
                    select x.Symbol).ToList();
            }
            
            public virtual object OnData(object slice) {
                if (slice.HasData) {
                    var symbol = slice.Keys[0];
                    if (symbol) {
                        if (this._blackList.Count > 50) {
                            this._blackList.pop(0);
                        }
                        this._blackList.append(symbol);
                    }
                }
            }
            
            public virtual object OnSecuritiesChanged(object changes) {
                // if we have no changes, do nothing
                if (changes == null) {
                    return;
                }
                // liquidate removed securities
                foreach (var security in changes.RemovedSecurities) {
                    if (security.Invested) {
                        this.Liquidate(security.Symbol);
                    }
                }
                foreach (var security in changes.AddedSecurities) {
                    this.SetHoldings(security.Symbol, 0.001);
                }
            }
        }
    }
}
