
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using date = datetime.date;

using System.Collections.Generic;

using System.Linq;

public static class CoarseFineFundamentalRegressionAlgorithm {
    
    static CoarseFineFundamentalRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class CoarseFineFundamentalRegressionAlgorithm
        : QCAlgorithm {
        
        public None changes;
        
        public int numberOfSymbolsFine;
        
        public virtual object Initialize() {
            this.SetStartDate(2014, 3, 24);
            this.SetEndDate(2014, 4, 7);
            this.SetCash(50000);
            this.UniverseSettings.Resolution = Resolution.Daily;
            // this add universe method accepts two parameters:
            // - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
            // - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
            this.AddUniverse(this.CoarseSelectionFunction, this.FineSelectionFunction);
            this.changes = null;
            this.numberOfSymbolsFine = 2;
        }
        
        // return a list of three fixed symbol objects
        public virtual object CoarseSelectionFunction(object coarse) {
            var tickers = new List<string> {
                "GOOG",
                "BAC",
                "SPY"
            };
            if (this.Time.date() < new date(2014, 4, 1)) {
                tickers = new List<string> {
                    "AAPL",
                    "AIG",
                    "IBM"
                };
            }
            return (from x in tickers
                select Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
        }
        
        // sort the data by market capitalization and take the top 'NumberOfSymbolsFine'
        public virtual object FineSelectionFunction(object fine) {
            // sort descending by market capitalization
            var sortedByMarketCap = fine.OrderByDescending(x => x.MarketCap).ToList();
            // take the top entries from our sorted collection
            return (from x in sortedByMarketCap[::self.numberOfSymbolsFine]
                select x.Symbol).ToList();
        }
        
        public virtual object OnData(object data) {
            // if we have no changes, do nothing
            if (this.changes == null) {
                return;
            }
            // liquidate removed securities
            foreach (var security in this.changes.RemovedSecurities) {
                if (security.Invested) {
                    this.Liquidate(security.Symbol);
                    this.Debug("Liquidated Stock: " + security.Symbol.Value.ToString());
                }
            }
            // we want 50% allocation in each security in our universe
            foreach (var security in this.changes.AddedSecurities) {
                if (security.Fundamentals.EarningRatios.EquityPerShareGrowth.OneYear > 0.25) {
                    this.SetHoldings(security.Symbol, 0.5);
                    this.Debug("Purchased Stock: " + security.Symbol.Value.ToString());
                }
            }
            this.changes = null;
        }
        
        // this event fires whenever we have changes to our universe
        public virtual object OnSecuritiesChanged(object changes) {
            this.changes = changes;
        }
    }
}
