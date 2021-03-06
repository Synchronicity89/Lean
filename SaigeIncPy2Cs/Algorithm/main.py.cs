
using clr;

public static class main {
    
    static main() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Common");
    }
    
    // Basic template algorithm simply initializes the date range and cash
    public class BasicTemplateAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Second);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        //         
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 1);
            }
        }
    }
}
