namespace AltData {
    
    using AddReference = clr.AddReference;
    
    public static class TradingEconomicsAlgorithm {
        
        static TradingEconomicsAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class TradingEconomicsAlgorithm
            : QCAlgorithm {
            
            public object interestRate;
            
            public virtual object Initialize() {
                this.SetStartDate(2013, 11, 1);
                this.SetEndDate(2019, 10, 3);
                this.SetCash(100000);
                this.AddEquity("AGG", Resolution.Hour);
                this.AddEquity("SPY", Resolution.Hour);
                this.interestRate = this.AddData(TradingEconomicsCalendar, TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol;
                // Request 365 days of interest rate history with the TradingEconomicsCalendar custom data Symbol.
                // We should expect no historical data because 2013-11-01 is before the absolute first point of data
                var history = this.History(TradingEconomicsCalendar, this.interestRate, 365, Resolution.Daily);
                // Count the amount of items we get from our history request (should be zero)
                this.Debug("We got {len(history)} items from our history request");
            }
            
            public virtual object OnData(object data) {
                // Make sure we have an interest rate calendar event
                if (!data.ContainsKey(this.interestRate)) {
                    return;
                }
                var announcement = data[this.interestRate];
                // Confirm its a FED Rate Decision
                if (announcement.Event != "Fed Interest Rate Decision") {
                    return;
                }
                // In the event of a rate increase, rebalance 50% to Bonds.
                var interestRateDecreased = announcement.Actual <= announcement.Previous;
                if (interestRateDecreased) {
                    this.SetHoldings("SPY", 1);
                    this.SetHoldings("AGG", 0);
                } else {
                    this.SetHoldings("SPY", 0.5);
                    this.SetHoldings("AGG", 0.5);
                }
            }
        }
    }
}
