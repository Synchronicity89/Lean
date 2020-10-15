
using AddReference = clr.AddReference;

using RsiAlphaModel = Alphas.RsiAlphaModel.RsiAlphaModel;

using EqualWeightingPortfolioConstructionModel = Portfolio.EqualWeightingPortfolioConstructionModel.EqualWeightingPortfolioConstructionModel;

using StandardDeviationExecutionModel = Execution.StandardDeviationExecutionModel.StandardDeviationExecutionModel;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class StandardDeviationExecutionModelRegressionAlgorithm {
    
    static StandardDeviationExecutionModelRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Regression algorithm for the StandardDeviationExecutionModel.
    //     This algorithm shows how the execution model works to split up orders and submit them
    //     only when the price is 2 standard deviations from the 60min mean (default model settings).
    public class StandardDeviationExecutionModelRegressionAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // Set requested data resolution
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
            this.SetAlpha(RsiAlphaModel(14, Resolution.Hour));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(StandardDeviationExecutionModel());
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log("{self.Time}: {orderEvent}");
        }
    }
}
