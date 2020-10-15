
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class RenkoConsolidatorAlgorithm {
    
    static RenkoConsolidatorAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Demonstration of how to initialize and use the RenkoConsolidator
    public class RenkoConsolidatorAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2012, 1, 1);
            this.SetEndDate(2013, 1, 1);
            this.AddEquity("SPY", Resolution.Daily);
            // this is the simple constructor that will perform the
            // renko logic to the Value property of the data it receives.
            // break SPY into $2.5 renko bricks and send that data to our 'OnRenkoBar' method
            var renkoClose = RenkoConsolidator(2.5);
            renkoClose.DataConsolidated += this.HandleRenkoClose;
            this.SubscriptionManager.AddConsolidator("SPY", renkoClose);
            // this is the full constructor that can accept a value selector and a volume selector
            // this allows us to perform the renko logic on values other than Close, even computed values!
            // break SPY into (2*o + h + l + 3*c)/7
            var renko7bar = RenkoConsolidator(2.5, x => (2 * x.Open + x.High + x.Low + 3 * x.Close) / 7, x => x.Volume);
            renko7bar.DataConsolidated += this.HandleRenko7Bar;
            this.SubscriptionManager.AddConsolidator("SPY", renko7bar);
        }
        
        // We're doing our analysis in the OnRenkoBar method, but the framework verifies that this method exists, so we define it.
        public virtual object OnData(object data) {
        }
        
        // This function is called by our renkoClose consolidator defined in Initialize()
        //         Args:
        //             data: The new renko bar produced by the consolidator
        public virtual object HandleRenkoClose(object sender, object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings(data.Symbol, 1);
            }
            this.Log("CLOSE - {data.Time} - {data.Open} {data.Close}");
        }
        
        // This function is called by our renko7bar consolidator defined in Initialize()
        //         Args:
        //             data: The new renko bar produced by the consolidator
        public virtual object HandleRenko7Bar(object sender, object data) {
            if (this.Portfolio.Invested) {
                this.Liquidate(data.Symbol);
            }
            this.Log("7BAR - {data.Time} - {data.Open} {data.Close}");
        }
    }
}
