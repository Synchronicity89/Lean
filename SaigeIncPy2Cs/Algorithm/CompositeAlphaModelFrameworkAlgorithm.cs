
using AddReference = clr.AddReference;

using RsiAlphaModel = Alphas.RsiAlphaModel.RsiAlphaModel;

using EmaCrossAlphaModel = Alphas.EmaCrossAlphaModel.EmaCrossAlphaModel;

using EqualWeightingPortfolioConstructionModel = Portfolio.EqualWeightingPortfolioConstructionModel.EqualWeightingPortfolioConstructionModel;

using timedelta = datetime.timedelta;

using np = numpy;

public static class CompositeAlphaModelFrameworkAlgorithm {
    
    static CompositeAlphaModelFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Show cases how to use the CompositeAlphaModel to define.
    public class CompositeAlphaModelFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // even though we're using a framework algorithm, we can still add our securities
            // using the AddEquity/Forex/Crypto/ect methods and then pass them into a manual
            // universe selection model using Securities.Keys
            this.AddEquity("SPY");
            this.AddEquity("IBM");
            this.AddEquity("BAC");
            this.AddEquity("AIG");
            // define a manual universe of all the securities we manually registered
            this.SetUniverseSelection(ManualUniverseSelectionModel());
            // define alpha model as a composite of the rsi and ema cross models
            this.SetAlpha(CompositeAlphaModel(RsiAlphaModel(), EmaCrossAlphaModel()));
            // default models for the rest
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(NullRiskManagementModel());
        }
    }
}
