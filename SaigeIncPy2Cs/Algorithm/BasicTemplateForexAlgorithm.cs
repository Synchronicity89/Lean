
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using np = numpy;

using System.Collections.Generic;

public static class BasicTemplateForexAlgorithm {
    
    static BasicTemplateForexAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateForexAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            // Set the cash we'd like to use for our backtest
            this.SetCash(100000);
            // Start and end dates for the backtest.
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            // Add FOREX contract you want to trade
            // find available contracts here https://www.quantconnect.com/data#forex/oanda/cfd
            this.AddForex("EURUSD", Resolution.Minute);
            this.AddForex("GBPUSD", Resolution.Minute);
            this.AddForex("EURGBP", Resolution.Minute);
            this.History(5, Resolution.Daily);
            this.History(5, Resolution.Hour);
            this.History(5, Resolution.Minute);
            var history = this.History(TimeSpan.FromSeconds(5), Resolution.Second);
            foreach (var data in history.OrderBy(x => x.Time).ToList()) {
                foreach (var key in data.Keys) {
                    this.Log(key.Value.ToString() + ": " + data.Time.ToString() + " > " + data[key].Value.ToString());
                }
            }
        }
        
        public virtual object OnData(object data) {
            // Print to console to verify that data is coming in
            foreach (var key in data.Keys) {
                this.Log(key.Value.ToString() + ": " + data.Time.ToString() + " > " + data[key].Value.ToString());
            }
        }
    }
}
