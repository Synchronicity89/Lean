
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class AddUniverseSelectionModelAlgorithm {
    
    static AddUniverseSelectionModelAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    public class AddUniverseSelectionModelAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 8);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            this.UniverseSettings.Resolution = Resolution.Daily;
            // set algorithm framework models
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(minutes: 20), 0.025, null));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetUniverseSelection(ManualUniverseSelectionModel(new List<object> {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            }));
            this.AddUniverseSelection(ManualUniverseSelectionModel(new List<object> {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA)
            }));
            this.AddUniverseSelection(ManualUniverseSelectionModel(Symbol.Create("SPY", SecurityType.Equity, Market.USA), Symbol.Create("FB", SecurityType.Equity, Market.USA)));
        }
        
        public virtual object OnEndOfAlgorithm() {
            if (this.UniverseManager.Count != 3) {
                throw new ValueError("Unexpected universe count");
            }
            if (this.UniverseManager.ActiveSecurities.Count != 3) {
                throw new ValueError("Unexpected active securities");
            }
        }
    }
}
