
using AddReference = clr.AddReference;

public static class FinancialAdvisorDemoAlgorithm {
    
    static FinancialAdvisorDemoAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class FinancialAdvisorDemoAlgorithm
        : QCAlgorithm {
        
        public object DefaultOrderProperties;
        
        public object symbol;
        
        public virtual object Initialize() {
            // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must be initialized.
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            this.symbol = this.AddEquity("SPY", Resolution.Second).Symbol;
            // The default order properties can be set here to choose the FA settings
            // to be automatically used in any order submission method (such as SetHoldings, Buy, Sell and Order)
            // Use a default FA Account Group with an Allocation Method
            this.DefaultOrderProperties = InteractiveBrokersOrderProperties();
            // account group created manually in IB/TWS
            this.DefaultOrderProperties.FaGroup = "TestGroupEQ";
            // supported allocation methods are: EqualQuantity, NetLiq, AvailableEquity, PctChange
            this.DefaultOrderProperties.FaMethod = "EqualQuantity";
            // set a default FA Allocation Profile
            // DefaultOrderProperties = InteractiveBrokersOrderProperties()
            // allocation profile created manually in IB/TWS
            // self.DefaultOrderProperties.FaProfile = "TestProfileP"
            // send all orders to a single managed account
            // DefaultOrderProperties = InteractiveBrokersOrderProperties()
            // a sub-account linked to the Financial Advisor master account
            // self.DefaultOrderProperties.Account = "DU123456"
        }
        
        public virtual object OnData(object data) {
            // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            if (!this.Portfolio.Invested) {
                // when logged into IB as a Financial Advisor, this call will use order properties
                // set in the DefaultOrderProperties property of QCAlgorithm
                this.SetHoldings("SPY", 1);
            }
        }
    }
}
