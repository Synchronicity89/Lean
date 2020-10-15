
using clr;

public static class BasicTemplateFillForwardAlgorithm {
    
    static BasicTemplateFillForwardAlgorithm() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Common");
    }
    
    // Basic template algorithm simply initializes the date range and cash
    public class BasicTemplateFillForwardAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 11, 30);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddSecurity(SecurityType.Equity, "ASUR", Resolution.Second);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        //         
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("ASUR", 1);
            }
        }
    }
}
