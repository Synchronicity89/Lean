
using AddReference = clr.AddReference;

using HistoricalReturnsAlphaModel = Alphas.HistoricalReturnsAlphaModel.HistoricalReturnsAlphaModel;

using UnconstrainedMeanVariancePortfolioOptimizer = Portfolio.UnconstrainedMeanVariancePortfolioOptimizer.UnconstrainedMeanVariancePortfolioOptimizer;

using NullRiskManagementModel = Risk.NullRiskManagementModel.NullRiskManagementModel;

using System.Collections.Generic;

using System.Linq;

public static class BlackLittermanPortfolioOptimizationFrameworkAlgorithm {
    
    static BlackLittermanPortfolioOptimizationFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Black-Litterman Optimization algorithm.
    public class BlackLittermanPortfolioOptimizationFrameworkAlgorithm
        : QCAlgorithm {
        
        public List<object> symbols;
        
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            this.symbols = (from x in new List<object> {
                "AIG",
                "BAC",
                "IBM",
                "SPY"
            }
                select Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
            var optimizer = UnconstrainedMeanVariancePortfolioOptimizer();
            // set algorithm framework models
            this.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(this.coarseSelector));
            this.SetAlpha(HistoricalReturnsAlphaModel(resolution: Resolution.Daily));
            this.SetPortfolioConstruction(BlackLittermanOptimizationPortfolioConstructionModel(optimizer: optimizer));
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(NullRiskManagementModel());
        }
        
        public virtual object coarseSelector(object coarse) {
            // Drops SPY after the 8th
            var last = this.Time.day > 8 ? 3 : this.symbols.Count;
            return this.symbols[0::last];
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Debug(orderEvent);
            }
        }
    }
}
