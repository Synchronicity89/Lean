
using RandomForestRegressor = sklearn.ensemble.RandomForestRegressor;

using train_test_split = sklearn.model_selection.train_test_split;

using np = numpy;

using System.Collections.Generic;

using System.Linq;

using System;

public static class RandomForextTemplateAlgo {
    
    public class AlphaFiveUSTreasuries
        : QCAlgorithm {
        
        public List<string> assets;
        
        public object portfolioValue;
        
        public Dictionary<object, object> symbols;
        
        public virtual object Initialize() {
            //1. Required: Five years of backtest history
            this.SetStartDate(2014, 1, 1);
            //2. Required: Alpha Streams Models:
            this.SetBrokerageModel(BrokerageName.AlphaStreams);
            //3. Required: Significant AUM Capacity
            this.SetCash(1000000);
            //4. Required: Benchmark to SPY
            this.SetBenchmark("SPY");
            this.SetPortfolioConstruction(InsightWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.assets = new List<string> {
                "IEF",
                "SHY",
                "TLT",
                "IEI",
                "SHV",
                "TLH",
                "EDV",
                "BIL",
                "SPTL",
                "TBT",
                "TMF",
                "TMV",
                "TBF",
                "VGSH",
                "VGIT",
                "VGLT",
                "SCHO",
                "SCHR",
                "SPTS",
                "GOVT"
            };
            this.symbols = new Dictionary<object, object> {
            };
            this.portfolioValue = RollingWindow[Decimal](500);
            this.SetWarmup(500);
            // Add Equity ------------------------------------------------ 
            foreach (var i in range(this.assets.Count)) {
                this.symbols[this.assets[i]] = this.AddEquity(this.assets[i], Resolution.Hour).Symbol;
            }
            this.Schedule.On(this.DateRules.Every(DayOfWeek.Monday), this.TimeRules.AfterMarketOpen("IEF", 30), this.EveryDayAfterMarketOpen);
        }
        
        public virtual object EveryDayAfterMarketOpen() {
            object symbol;
            object insights;
            if (!this.Portfolio.Invested) {
                insights = new List<object>();
                foreach (var _tup_1 in this.symbols.items()) {
                    var ticker = _tup_1.Item1;
                    symbol = _tup_1.Item2;
                    insights.append(Insight.Price(symbol, timedelta(days: 5), InsightDirection.Up, 0.01, null, null, 1 / this.symbols.Count));
                }
                this.EmitInsights(insights);
            } else {
                var qb = this;
                //==============================
                // Initialize instance of Random Forest Regressor
                var regressor = RandomForestRegressor(n_estimators: 100, min_samples_split: 5, random_state: 1990);
                // Fetch history on our universe
                var df = qb.History(qb.Securities.Keys, 500, Resolution.Hour);
                // Get train/test data
                var returns = df.unstack(level: 1).close.transpose().pct_change().dropna();
                var X = returns;
                var y = (from x in qb.portfolioValue
                    select x).ToList()[-X.shape[0]];
                var _tup_2 = train_test_split(X, y, test_size: 0.2, random_state: 1990);
                var X_train = _tup_2.Item1;
                var X_test = _tup_2.Item2;
                var y_train = _tup_2.Item3;
                var y_test = _tup_2.Item4;
                // Fit regressor
                regressor.fit(X_train, y_train);
                // Get long-only predictions
                var weights = regressor.feature_importances_;
                var symbols = returns.columns[np.where(weights)];
                var selected = zip(symbols, weights);
                // ==============================
                insights = new List<object>();
                foreach (var _tup_3 in selected) {
                    symbol = _tup_3.Item1;
                    var weight = _tup_3.Item2;
                    insights.append(Insight.Price(symbol, timedelta(days: 5), InsightDirection.Up, 0.01, null, null, weight));
                }
                this.EmitInsights(insights);
            }
        }
        
        public virtual object OnData(object data) {
            this.portfolioValue.Add(this.Portfolio.TotalPortfolioValue);
        }
    }
}
