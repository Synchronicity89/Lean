
using AddReference = clr.AddReference;

using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;

using ImmediateExecutionModel = QuantConnect.Algorithm.Framework.Execution.ImmediateExecutionModel;

using UncorrelatedUniverseSelectionModel = Selection.UncorrelatedUniverseSelectionModel.UncorrelatedUniverseSelectionModel;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class UncorrelatedUniverseSelectionFrameworkAlgorithm {
    
    static UncorrelatedUniverseSelectionFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    public class UncorrelatedUniverseSelectionFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2018, 1, 1);
            this.SetCash(1000000);
            var benchmark = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            this.SetUniverseSelection(UncorrelatedUniverseSelectionModel(benchmark));
            this.SetAlpha(new UncorrelatedUniverseSelectionAlphaModel());
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
        }
    }
    
    // Uses ranking of intraday percentage difference between open price and close price to create magnitude and direction prediction for insights
    public class UncorrelatedUniverseSelectionAlphaModel
        : AlphaModel {
        
        public object numberOfStocks;
        
        public object predictionInterval;
        
        public UncorrelatedUniverseSelectionAlphaModel(object numberOfStocks = 10, object predictionInterval = timedelta(1)) {
            this.predictionInterval = predictionInterval;
            this.numberOfStocks = numberOfStocks;
        }
        
        public virtual object Update(object algorithm, object data) {
            var symbolsRet = new dict();
            foreach (var kvp in algorithm.ActiveSecurities) {
                var security = kvp.Value;
                if (security.HasData) {
                    var open = security.Open;
                    if (open != 0) {
                        symbolsRet[security.Symbol] = security.Close / open - 1;
                    }
                }
            }
            // Rank on the absolute value of price change
            symbolsRet = new dict(symbolsRet.items().OrderByDescending(kvp => abs(kvp[1])).ToList()[::self.numberOfStocks]);
            var insights = new List<object>();
            foreach (var _tup_1 in symbolsRet.items()) {
                var symbol = _tup_1.Item1;
                var price_change = _tup_1.Item2;
                // Emit "up" insight if the price change is positive and "down" otherwise
                var direction = price_change > 0 ? InsightDirection.Up : InsightDirection.Down;
                insights.append(Insight.Price(symbol, this.predictionInterval, direction, abs(price_change), null));
            }
            return insights;
        }
    }
}
