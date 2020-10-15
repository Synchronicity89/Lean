
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class AddRiskManagementAlgorithm {
    
    static AddRiskManagementAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Basic template framework algorithm uses framework components to define the algorithm.
    public class AddRiskManagementAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            var symbols = new List<object> {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            };
            // set algorithm framework models
            this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(minutes: 20), 0.025, null));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            // Both setting methods should work
            var riskModel = CompositeRiskManagementModel(MaximumDrawdownPercentPortfolio(0.02));
            riskModel.AddRiskManagement(MaximumUnrealizedProfitPercentPerSecurity(0.01));
            this.SetRiskManagement(MaximumDrawdownPercentPortfolio(0.02));
            this.AddRiskManagement(MaximumUnrealizedProfitPercentPerSecurity(0.01));
        }
    }
}
