
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class AddAlphaModelAlgorithm {
    
    static AddAlphaModelAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    public class AddAlphaModelAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            this.UniverseSettings.Resolution = Resolution.Daily;
            var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var fb = Symbol.Create("FB", SecurityType.Equity, Market.USA);
            var ibm = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            // set algorithm framework models
            this.SetUniverseSelection(ManualUniverseSelectionModel(new List<object> {
                spy,
                fb,
                ibm
            }));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.AddAlpha(new OneTimeAlphaModel(spy));
            this.AddAlpha(new OneTimeAlphaModel(fb));
            this.AddAlpha(new OneTimeAlphaModel(ibm));
        }
    }
    
    public class OneTimeAlphaModel
        : AlphaModel {
        
        public object symbol;
        
        public bool triggered;
        
        public OneTimeAlphaModel(object symbol) {
            this.symbol = symbol;
            this.triggered = false;
        }
        
        public virtual object Update(object algorithm, object data) {
            var insights = new List<object>();
            if (!this.triggered) {
                this.triggered = true;
                insights.append(Insight.Price(this.symbol, Resolution.Daily, 1, InsightDirection.Down));
            }
            return insights;
        }
    }
}
