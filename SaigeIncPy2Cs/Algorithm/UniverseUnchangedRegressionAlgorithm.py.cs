
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using Universe = QuantConnect.Data.UniverseSelection.Universe;

using date = datetime.date;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class UniverseUnchangedRegressionAlgorithm {
    
    static UniverseUnchangedRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class UniverseUnchangedRegressionAlgorithm
        : QCAlgorithm {
        
        public int numberOfSymbolsFine;
        
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2014, 3, 25);
            this.SetEndDate(2014, 4, 7);
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(days: 1), 0.025, null));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.AddUniverse(this.CoarseSelectionFunction, this.FineSelectionFunction);
            this.numberOfSymbolsFine = 2;
        }
        
        public virtual object CoarseSelectionFunction(object coarse) {
            // the first and second selection
            if (this.Time.date() <= new date(2014, 3, 26)) {
                var tickers = new List<string> {
                    "AAPL",
                    "AIG",
                    "IBM"
                };
                return (from x in tickers
                    select Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
            }
            // will skip fine selection
            return Universe.Unchanged;
        }
        
        public virtual object FineSelectionFunction(object fine) {
            if (this.Time.date() == new date(2014, 3, 25)) {
                var sortedByPeRatio = fine.OrderByDescending(x => x.ValuationRatios.PERatio).ToList();
                return (from x in sortedByPeRatio[::self.numberOfSymbolsFine]
                    select x.Symbol).ToList();
            }
            // the second selection will return unchanged, in the following fine selection will be skipped
            return Universe.Unchanged;
        }
        
        // assert security changes, throw if called more than once
        public virtual object OnSecuritiesChanged(object changes) {
            var addedSymbols = (from x in changes.AddedSecurities
                select x.Symbol).ToList();
            if (changes.AddedSecurities.Count != 2 || this.Time.date() != new date(2014, 3, 25) || !addedSymbols.Contains(Symbol.Create("AAPL", SecurityType.Equity, Market.USA)) || !addedSymbols.Contains(Symbol.Create("IBM", SecurityType.Equity, Market.USA))) {
                throw new ValueError("Unexpected security changes");
            }
            this.Log("OnSecuritiesChanged({self.Time}):: {changes}");
        }
    }
}
