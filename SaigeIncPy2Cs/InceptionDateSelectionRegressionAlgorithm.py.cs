
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class InceptionDateSelectionRegressionAlgorithm {
    
    static InceptionDateSelectionRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
    }
    
    public class InceptionDateSelectionRegressionAlgorithm
        : QCAlgorithm {
        
        public None changes;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 1);
            this.SetEndDate(2013, 10, 31);
            this.SetCash(100000);
            this.changes = null;
            this.UniverseSettings.Resolution = Resolution.Hour;
            // select IBM once a week, empty universe the other days
            this.AddUniverseSelection(CustomUniverseSelectionModel("my-custom-universe", dt => dt.day % 7 == 0 ? new List<object> {
                "IBM"
            } : new List<object>()));
            // Adds SPY 5 days after StartDate and keep it in Universe
            this.AddUniverseSelection(InceptionDateUniverseSelectionModel("spy-inception", new Dictionary<object, object> {
                {
                    "SPY",
                    this.StartDate + new timedelta(5)}}));
        }
        
        public virtual object OnData(object slice) {
            if (this.changes == null) {
                return;
            }
            // we'll simply go long each security we added to the universe
            foreach (var security in this.changes.AddedSecurities) {
                this.SetHoldings(security.Symbol, 0.5);
            }
            this.changes = null;
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            // liquidate removed securities
            foreach (var security in changes.RemovedSecurities) {
                this.Liquidate(security.Symbol, "Removed from Universe");
            }
            this.changes = changes;
        }
    }
}
