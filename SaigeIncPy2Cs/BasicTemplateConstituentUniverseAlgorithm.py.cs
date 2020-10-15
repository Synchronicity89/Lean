
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class BasicTemplateConstituentUniverseAlgorithm {
    
    static BasicTemplateConstituentUniverseAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateConstituentUniverseAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            // by default will use algorithms UniverseSettings
            this.AddUniverse(this.Universe.Constituent.Steel());
            // we specify the UniverseSettings it should use
            this.AddUniverse(this.Universe.Constituent.AggressiveGrowth(UniverseSettings(Resolution.Hour, 2, false, false, this.UniverseSettings.MinimumTimeInUniverse)));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            this.SetExecution(ImmediateExecutionModel());
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
        }
    }
}
