
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using np = numpy;

using pd = pandas;

using ScheduledUniverse = QuantConnect.Data.UniverseSelection.ScheduledUniverse;

using UniverseSelectionModel = Selection.UniverseSelectionModel.UniverseSelectionModel;

using System.Collections.Generic;

using System.Linq;

public static class CapmAlphaRankingFrameworkAlgorithm {
    
    static CapmAlphaRankingFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // CapmAlphaRankingFrameworkAlgorithm: example of custom scheduled universe selection model
    public class CapmAlphaRankingFrameworkAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2016, 1, 1);
            this.SetEndDate(2017, 1, 1);
            this.SetCash(100000);
            // set algorithm framework models
            this.SetUniverseSelection(new CapmAlphaRankingUniverseSelectionModel());
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(1), 0.025, null));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01));
        }
    }
    
    // This universe selection model picks stocks with the highest alpha: interception of the linear regression against a benchmark.
    public class CapmAlphaRankingUniverseSelectionModel
        : UniverseSelectionModel {
        
        public string benchmark;
        
        public int period;
        
        public List<object> symbols;
        
        public int period = 21;
        
        public string benchmark = "SPY";
        
        public List<object> symbols = (from x in new List<object> {
            "AAPL",
            "AXP",
            "BA",
            "CAT",
            "CSCO",
            "CVX",
            "DD",
            "DIS",
            "GE",
            "GS",
            "HD",
            "IBM",
            "INTC",
            "JPM",
            "KO",
            "MCD",
            "MMM",
            "MRK",
            "MSFT",
            "NKE",
            "PFE",
            "PG",
            "TRV",
            "UNH",
            "UTX",
            "V",
            "VZ",
            "WMT",
            "XOM"
        }
            select Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
        
        public virtual object CreateUniverses(object algorithm) {
            // Adds the benchmark to the user defined universe
            var benchmark = algorithm.AddEquity(this.benchmark, Resolution.Daily);
            // Defines a schedule universe that fires after market open when the month starts
            return new List<object> {
                ScheduledUniverse(benchmark.Exchange.TimeZone, algorithm.DateRules.MonthStart(this.benchmark), algorithm.TimeRules.AfterMarketOpen(this.benchmark), datetime => this.SelectPair(algorithm, datetime), algorithm.UniverseSettings, algorithm.SecurityInitializer)
            };
        }
        
        // Selects the pair (two stocks) with the highest alpha
        public virtual object SelectPair(object algorithm, object date) {
            var dictionary = new dict();
            var benchmark = this._getReturns(algorithm, this.benchmark);
            var ones = np.ones(benchmark.Count);
            foreach (var symbol in this.symbols) {
                var prices = this._getReturns(algorithm, symbol);
                if (prices == null) {
                    continue;
                }
                var A = np.vstack(new List<object> {
                    prices,
                    ones
                }).T;
                // Calculate the Least-Square fitting to the returns of a given symbol and the benchmark
                var ols = np.linalg.lstsq(A, benchmark)[0];
                dictionary[symbol] = ols[1];
            }
            // Returns the top 2 highest alphas
            var orderedDictionary = dictionary.items().OrderByDescending(x => x[1]).ToList();
            return (from x in orderedDictionary[::2]
                select x[0]).ToList();
        }
        
        public virtual object _getReturns(object algorithm, object symbol) {
            var history = algorithm.History(new List<object> {
                symbol
            }, this.period, Resolution.Daily);
            if (history.empty) {
                return null;
            }
            var window = RollingWindow[float](this.period);
            var rateOfChange = RateOfChange(1);
            Func<object, object, object> roc_updated = (s,item) => {
                window.Add(item.Value);
            };
            rateOfChange.Updated += roc_updated;
            history = history.close.reset_index(level: 0, drop: true);
            foreach (var _tup_1 in history) {
                var time = _tup_1.Item1;
                var value = _tup_1.Item2;
                rateOfChange.Update(time, value);
            }
            return (from x in window
                select x).ToList();
        }
    }
}
