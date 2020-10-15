
using AddReference = clr.AddReference;

public static class DynamicSecurityDataAlgorithm {
    
    static DynamicSecurityDataAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class DynamicSecurityDataAlgorithm
        : QCAlgorithm {
        
        public object GOOGL;
        
        public string Ticker;
        
        public virtual object Initialize() {
            this.Ticker = "GOOGL";
            this.SetStartDate(2015, 10, 22);
            this.SetEndDate(2015, 10, 30);
            this.GOOGL = this.AddEquity(this.Ticker, Resolution.Daily);
            this.AddData(SECReport8K, this.Ticker, Resolution.Daily);
            this.AddData(SECReport10K, this.Ticker, Resolution.Daily);
            this.AddData(SECReport10Q, this.Ticker, Resolution.Daily);
        }
        
        public virtual object OnData(object data) {
            // The Security object's Data property provides convenient access
            // to the various types of data related to that security. You can
            // access not only the security's price data, but also any custom
            // data that is mapped to the security, such as our SEC reports.
            // 1. Get the most recent data point of a particular type:
            // 1.a Using the generic method, Get(T): => T
            var googlSec8kReport = this.GOOGL.Data.Get(SECReport8K);
            var googlSec10kReport = this.GOOGL.Data.Get(SECReport10K);
            this.Log("{}:  8K: {}".format(this.Time, googlSec8kReport));
            this.Log("{}: 10K: {}".format(this.Time, googlSec10kReport));
            // 2. Get the list of data points of a particular type for the most recent time step:
            // 2.a Using the generic method, GetAll(T): => IReadOnlyList<T>
            var googlSec8kReports = this.GOOGL.Data.GetAll(SECReport8K);
            var googlSec10kReports = this.GOOGL.Data.GetAll(SECReport10K);
            this.Log("{}:  8K: {}".format(this.Time, googlSec8kReports.Count));
            this.Log("{}: 10K: {}".format(this.Time, googlSec10kReports.Count));
            if (!this.Portfolio.Invested) {
                this.Buy(this.GOOGL.Symbol, 10);
            }
        }
    }
}
