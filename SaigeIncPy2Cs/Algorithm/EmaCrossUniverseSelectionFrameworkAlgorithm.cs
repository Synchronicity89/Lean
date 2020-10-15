
using AddReference = clr.AddReference;

using ConstantAlphaModel = Alphas.ConstantAlphaModel.ConstantAlphaModel;

using EmaCrossUniverseSelectionModel = Selection.EmaCrossUniverseSelectionModel.EmaCrossUniverseSelectionModel;

using EqualWeightingPortfolioConstructionModel = Portfolio.EqualWeightingPortfolioConstructionModel.EqualWeightingPortfolioConstructionModel;

using timedelta = datetime.timedelta;

public static class EmaCrossUniverseSelectionFrameworkAlgorithm {
    
    static EmaCrossUniverseSelectionFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Framework algorithm that uses the EmaCrossUniverseSelectionModel to select the universe based on a moving average cross.
    public class EmaCrossUniverseSelectionFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.SetCash(100000);
            var fastPeriod = 100;
            var slowPeriod = 300;
            var count = 10;
            this.UniverseSettings.Leverage = 2.0;
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetUniverseSelection(EmaCrossUniverseSelectionModel(fastPeriod, slowPeriod, count));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(1), null, null));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
        }
    }
}
