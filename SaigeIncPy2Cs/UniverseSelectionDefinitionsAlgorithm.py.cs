
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using timedelta = datetime.timedelta;

public static class UniverseSelectionDefinitionsAlgorithm {
    
    static UniverseSelectionDefinitionsAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class UniverseSelectionDefinitionsAlgorithm
        : QCAlgorithm {
        
        public None changes;
        
        public virtual object Initialize() {
            // subscriptions added via universe selection will have this resolution
            this.UniverseSettings.Resolution = Resolution.Hour;
            // force securities to remain in the universe for a minimm of 30 minutes
            this.UniverseSettings.MinimumTimeInUniverse = new timedelta(minutes: 30);
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // add universe for the top 50 stocks by dollar volume
            this.AddUniverse(this.Universe.DollarVolume.Top(50));
            // add universe for the bottom 50 stocks by dollar volume
            this.AddUniverse(this.Universe.DollarVolume.Bottom(50));
            // add universe for the 90th dollar volume percentile
            this.AddUniverse(this.Universe.DollarVolume.Percentile(90.0));
            // add universe for stocks between the 70th and 80th dollar volume percentile
            this.AddUniverse(this.Universe.DollarVolume.Percentile(70.0, 80.0));
            this.changes = null;
        }
        
        public virtual object OnData(object data) {
            if (this.changes == null) {
                return;
            }
            // liquidate securities that fell out of our universe
            foreach (var security in this.changes.RemovedSecurities) {
                if (security.Invested) {
                    this.Liquidate(security.Symbol);
                }
            }
            // invest in securities just added to our universe
            foreach (var security in this.changes.AddedSecurities) {
                if (!security.Invested) {
                    this.MarketOrder(security.Symbol, 10);
                }
            }
            this.changes = null;
        }
        
        // this event fires whenever we have changes to our universe
        public virtual object OnSecuritiesChanged(object changes) {
            this.changes = changes;
        }
    }
}
