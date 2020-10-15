
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class OptionDataNullReferenceRegressionAlgorithm {
    
    static OptionDataNullReferenceRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class OptionDataNullReferenceRegressionAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2016, 12, 1);
            this.SetEndDate(2017, 1, 1);
            this.SetCash(500000);
            this.AddEquity("DUST");
            var option = this.AddOption("DUST");
            option.SetFilter(this.UniverseFunc);
        }
        
        public virtual object UniverseFunc(object universe) {
            return universe.IncludeWeeklys().Strikes(-1, +1).Expiration(new timedelta(25), new timedelta(100));
        }
    }
}
