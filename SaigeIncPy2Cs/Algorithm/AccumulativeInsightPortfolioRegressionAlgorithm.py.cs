
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class AccumulativeInsightPortfolioRegressionAlgorithm {
    
    static AccumulativeInsightPortfolioRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    public class AccumulativeInsightPortfolioRegressionAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            var symbols = new List<object> {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            };
            // set algorithm framework models
            this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(minutes: 20), 0.025, 0.25));
            this.SetPortfolioConstruction(AccumulativeInsightPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
        }
        
        public virtual object OnEndOfAlgorithm() {
            // holdings value should be 0.03 - to avoid price fluctuation issue we compare with 0.06 and 0.01
            if (this.Portfolio.TotalHoldingsValue > this.Portfolio.TotalPortfolioValue * 0.06 || this.Portfolio.TotalHoldingsValue < this.Portfolio.TotalPortfolioValue * 0.01) {
                throw new ValueError("Unexpected Total Holdings Value: " + this.Portfolio.TotalHoldingsValue.ToString());
            }
        }
    }
}
