
using AddReference = clr.AddReference;

using TrailingStopRiskManagementModel = Risk.TrailingStopRiskManagementModel.TrailingStopRiskManagementModel;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class TrailingStopRiskFrameworkAlgorithm {
    
    static TrailingStopRiskFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Show example of how to use the TrailingStopRiskManagementModel
    public class TrailingStopRiskFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // set algorithm framework models
            this.SetUniverseSelection(ManualUniverseSelectionModel(new List<object> {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            }));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(minutes: 20), 0.025, null));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(TrailingStopRiskManagementModel(0.01));
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Debug("Processed Order: {orderEvent.Symbol}, Quantity: {orderEvent.FillQuantity}");
            }
        }
    }
}
