
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using System.Collections.Generic;

public static class WeeklyUniverseSelectionRegressionAlgorithm {
    
    static WeeklyUniverseSelectionRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class WeeklyUniverseSelectionRegressionAlgorithm
        : QCAlgorithm {
        
        public None changes;
        
        public virtual object Initialize() {
            this.SetCash(100000);
            this.SetStartDate(2013, 10, 1);
            this.SetEndDate(2013, 10, 31);
            this.UniverseSettings.Resolution = Resolution.Hour;
            // select IBM once a week, empty universe the other days
            this.AddUniverse("my-custom-universe", dt => dt.day % 7 == 0 ? new List<object> {
                "IBM"
            } : new List<object>());
        }
        
        public virtual object OnData(object slice) {
            if (this.changes == null) {
                return;
            }
            // liquidate removed securities
            foreach (var security in this.changes.RemovedSecurities) {
                if (security.Invested) {
                    this.Log("{} Liquidate {}".format(this.Time, security.Symbol));
                    this.Liquidate(security.Symbol);
                }
            }
            // we'll simply go long each security we added to the universe
            foreach (var security in this.changes.AddedSecurities) {
                if (!security.Invested) {
                    this.Log("{} Buy {}".format(this.Time, security.Symbol));
                    this.SetHoldings(security.Symbol, 1);
                }
            }
            this.changes = null;
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            this.changes = changes;
        }
    }
}
