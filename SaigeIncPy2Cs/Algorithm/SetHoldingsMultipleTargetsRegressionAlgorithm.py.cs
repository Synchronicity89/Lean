
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class SetHoldingsMultipleTargetsRegressionAlgorithm {
    
    static SetHoldingsMultipleTargetsRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class SetHoldingsMultipleTargetsRegressionAlgorithm
        : QCAlgorithm {
        
        public object _ibm;
        
        public object _spy;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            // use leverage 1 so we test the margin impact ordering
            this._spy = this.AddEquity("SPY", Resolution.Minute, Market.USA, false, 1).Symbol;
            this._ibm = this.AddEquity("IBM", Resolution.Minute, Market.USA, false, 1).Symbol;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings(new List<object> {
                    PortfolioTarget(this._spy, 0.8),
                    PortfolioTarget(this._ibm, 0.2)
                });
            } else {
                this.SetHoldings(new List<object> {
                    PortfolioTarget(this._ibm, 0.8),
                    PortfolioTarget(this._spy, 0.2)
                });
            }
        }
    }
}
