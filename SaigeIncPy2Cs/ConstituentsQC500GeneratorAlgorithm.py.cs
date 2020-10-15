
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

public static class ConstituentsQC500GeneratorAlgorithm {
    
    static ConstituentsQC500GeneratorAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class ConstituentsQC500GeneratorAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2018, 1, 1);
            this.SetEndDate(2019, 1, 1);
            this.SetCash(100000);
            // Add QC500 Universe
            this.AddUniverse(this.Universe.Index.QC500);
        }
    }
}
