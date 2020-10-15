
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using np = numpy;

using System.Collections.Generic;

public static class BasicTemplateFrameworkAlgorithm {
    
    static BasicTemplateFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Basic template framework algorithm uses framework components to define the algorithm.
    public class BasicTemplateFrameworkAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            var symbols = new List<object> {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            };
            // set algorithm framework models
            this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(minutes: 20), 0.025, null));
            // We can define who often the EWPCM will rebalance if no new insight is submitted using:
            // Resolution Enum:
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(Resolution.Daily));
            // timedelta
            // self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(timedelta(2)))
            // A lamdda datetime -> datetime. In this case, we can use the pre-defined func at Expiry helper class
            // self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(Expiry.EndOfWeek))
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01));
            this.Debug("numpy test >>> print numpy.pi: " + np.pi.ToString());
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Debug("Purchased Stock: {0}".format(orderEvent.Symbol));
            }
        }
    }
}
