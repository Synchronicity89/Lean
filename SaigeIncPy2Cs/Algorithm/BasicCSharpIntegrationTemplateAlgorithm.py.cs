
using AddReference = clr.AddReference;

using np = numpy;

public static class BasicCSharpIntegrationTemplateAlgorithm {
    
    static BasicCSharpIntegrationTemplateAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
        AddReference("System.Windows.Forms");
    }
    
    public class BasicCSharpIntegrationTemplateAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
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
                //# Calculate value of sin(10) for both python and C#
                this.Debug("According to Python, the value of sin(10) is {np.sin(10)}");
                this.Debug("According to C#, the value of sin(10) is {Math.Sin(10)}");
            }
        }
    }
}
