
using AddReference = clr.AddReference;

using CompositeRiskManagementModel = Risk.CompositeRiskManagementModel.CompositeRiskManagementModel;

using MaximumDrawdownPercentPortfolio = Risk.MaximumDrawdownPercentPortfolio.MaximumDrawdownPercentPortfolio;

using timedelta = datetime.timedelta;

using np = numpy;

using System.Collections.Generic;

public static class MaximumPortfolioDrawdownFrameworkAlgorithm {
    
    static MaximumPortfolioDrawdownFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Show example of how to use the MaximumDrawdownPercentPortfolio Risk Management Model
    public class MaximumPortfolioDrawdownFrameworkAlgorithm
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
            // define risk management model as a composite of several risk management models
            this.SetRiskManagement(CompositeRiskManagementModel(MaximumDrawdownPercentPortfolio(0.01), MaximumDrawdownPercentPortfolio(0.015, true)));
        }
    }
}
