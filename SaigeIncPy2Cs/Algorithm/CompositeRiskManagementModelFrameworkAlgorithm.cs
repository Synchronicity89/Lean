
using AddReference = clr.AddReference;

using CompositeRiskManagementModel = Risk.CompositeRiskManagementModel.CompositeRiskManagementModel;

using MaximumUnrealizedProfitPercentPerSecurity = Risk.MaximumUnrealizedProfitPercentPerSecurity.MaximumUnrealizedProfitPercentPerSecurity;

using MaximumDrawdownPercentPerSecurity = Risk.MaximumDrawdownPercentPerSecurity.MaximumDrawdownPercentPerSecurity;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class CompositeRiskManagementModelFrameworkAlgorithm {
    
    static CompositeRiskManagementModelFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Show cases how to use the CompositeRiskManagementModel.
    public class CompositeRiskManagementModelFrameworkAlgorithm
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
            this.SetRiskManagement(CompositeRiskManagementModel(MaximumUnrealizedProfitPercentPerSecurity(0.01), MaximumDrawdownPercentPerSecurity(0.01)));
        }
    }
}
