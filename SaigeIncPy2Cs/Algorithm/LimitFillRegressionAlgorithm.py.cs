
using AddReference = clr.AddReference;

public static class LimitFillRegressionAlgorithm {
    
    static LimitFillRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class LimitFillRegressionAlgorithm
        : QCAlgorithm {
        
        public int mid_datetime;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Second);
            this.mid_datetime = this.StartDate + (this.EndDate - this.StartDate) / 2;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (data.ContainsKey("SPY")) {
                if (this.IsRoundHour(this.Time)) {
                    var negative = this.Time < this.mid_datetime ? 1 : -1;
                    this.LimitOrder("SPY", negative * 10, data["SPY"].Price);
                }
            }
        }
        
        // Verify whether datetime is round hour
        public virtual object IsRoundHour(object dateTime) {
            return dateTime.minute == 0 && dateTime.second == 0;
        }
    }
}
