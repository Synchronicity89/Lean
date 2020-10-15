
using AddReference = clr.AddReference;

using EqualWeightingPortfolioConstructionModel = Portfolio.EqualWeightingPortfolioConstructionModel.EqualWeightingPortfolioConstructionModel;

using ConstantAlphaModel = Alphas.ConstantAlphaModel.ConstantAlphaModel;

using ImmediateExecutionModel = Execution.ImmediateExecutionModel.ImmediateExecutionModel;

using MaximumSectorExposureRiskManagementModel = Risk.MaximumSectorExposureRiskManagementModel.MaximumSectorExposureRiskManagementModel;

using date = datetime.date;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class SectorExposureRiskFrameworkAlgorithm {
    
    static SectorExposureRiskFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // This example algorithm defines its own custom coarse/fine fundamental selection model
    // ### with equally weighted portfolio and a maximum sector exposure.
    public class SectorExposureRiskFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2014, 3, 25);
            this.SetEndDate(2014, 4, 7);
            this.SetCash(100000);
            // set algorithm framework models
            this.SetUniverseSelection(FineFundamentalUniverseSelectionModel(this.SelectCoarse, this.SelectFine));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(1)));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetRiskManagement(MaximumSectorExposureRiskManagementModel());
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Debug("Order event: {orderEvent}. Holding value: {self.Securities[orderEvent.Symbol].Holdings.AbsoluteHoldingsValue}");
            }
        }
        
        public virtual object SelectCoarse(object coarse) {
            var tickers = this.Time.date() < new date(2014, 4, 1) ? new List<string> {
                "AAPL",
                "AIG",
                "IBM"
            } : new List<string> {
                "GOOG",
                "BAC",
                "SPY"
            };
            return (from x in tickers
                select Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
        }
        
        public virtual object SelectFine(object fine) {
            return (from f in fine
                select f.Symbol).ToList();
        }
    }
}
