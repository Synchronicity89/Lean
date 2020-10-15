
using AddReference = clr.AddReference;

public static class TradingEconomicsCalendarIndicatorAlgorithm {
    
    static TradingEconomicsCalendarIndicatorAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class TradingEconomicsCalendarIndicatorAlgorithm
        : QCAlgorithm {
        
        public object calendar;
        
        public object indicator;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2018, 1, 1);
            this.SetEndDate(2019, 1, 1);
            this.calendar = this.AddData(TradingEconomicsCalendar, TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol;
            this.indicator = this.AddData(TradingEconomicsIndicator, TradingEconomics.Indicator.UnitedStates.InterestRate).Symbol;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object slice) {
            if (slice.ContainsKey(this.calendar)) {
                this.Log("{self.Time} - {slice[self.calendar]}");
            }
            if (slice.ContainsKey(this.indicator)) {
                this.Log("{self.Time} - {slice[self.indicator]}");
            }
        }
    }
}
