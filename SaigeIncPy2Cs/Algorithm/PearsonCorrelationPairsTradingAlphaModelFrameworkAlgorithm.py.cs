
using AddReference = clr.AddReference;

public static class PearsonCorrelationPairsTradingAlphaModelFrameworkAlgorithm {
    
    static PearsonCorrelationPairsTradingAlphaModelFrameworkAlgorithm() {
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Framework algorithm that uses the PearsonCorrelationPairsTradingAlphaModel.
    //     This model extendes BasePairsTradingAlphaModel and uses Pearson correlation
    //     to rank the pairs trading candidates and use the best candidate to trade.
    public class PearsonCorrelationPairsTradingAlphaModelFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetUniverseSelection(ManualUniverseSelectionModel(Symbol.Create("AIG", SecurityType.Equity, Market.USA), Symbol.Create("BAC", SecurityType.Equity, Market.USA), Symbol.Create("IBM", SecurityType.Equity, Market.USA), Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            this.SetAlpha(PearsonCorrelationPairsTradingAlphaModel(252, Resolution.Daily));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(NullRiskManagementModel());
        }
    }
}
