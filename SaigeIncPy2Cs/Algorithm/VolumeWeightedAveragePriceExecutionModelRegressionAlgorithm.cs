
using AddReference = clr.AddReference;

using RsiAlphaModel = Alphas.RsiAlphaModel.RsiAlphaModel;

using EqualWeightingPortfolioConstructionModel = Portfolio.EqualWeightingPortfolioConstructionModel.EqualWeightingPortfolioConstructionModel;

using VolumeWeightedAveragePriceExecutionModel = Execution.VolumeWeightedAveragePriceExecutionModel.VolumeWeightedAveragePriceExecutionModel;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class VolumeWeightedAveragePriceExecutionModelRegressionAlgorithm {
    
    static VolumeWeightedAveragePriceExecutionModelRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Regression algorithm for the VolumeWeightedAveragePriceExecutionModel.
    //     This algorithm shows how the execution model works to split up orders and
    //     submit them only when the price is on the favorable side of the intraday VWAP.
    public class VolumeWeightedAveragePriceExecutionModelRegressionAlgorithm
        : QCAlgorithm {
        
        public object InsightsGenerated;
        
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(1000000);
            this.SetUniverseSelection(ManualUniverseSelectionModel(new List<object> {
                Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            }));
            // using hourly rsi to generate more insights
            this.SetAlpha(RsiAlphaModel(14, Resolution.Hour));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(VolumeWeightedAveragePriceExecutionModel());
            this.InsightsGenerated += this.OnInsightsGenerated;
        }
        
        public virtual object OnInsightsGenerated(object algorithm, object data) {
            this.Log("{self.Time}: {', '.join(str(x) for x in data.Insights)}");
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log("{self.Time}: {orderEvent}");
        }
    }
}
