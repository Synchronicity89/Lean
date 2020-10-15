
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class ScheduledEventsAlgorithm {
    
    static ScheduledEventsAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class ScheduledEventsAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY");
            // events are scheduled using date and time rules
            // date rules specify on what dates and event will fire
            // time rules specify at what time on thos dates the event will fire
            // schedule an event to fire at a specific date/time
            this.Schedule.On(this.DateRules.On(2013, 10, 7), this.TimeRules.At(13, 0), this.SpecificTime);
            // schedule an event to fire every trading day for a security the
            // time rule here tells it to fire 10 minutes after SPY's market open
            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", 10), this.EveryDayAfterMarketOpen);
            // schedule an event to fire every trading day for a security the
            // time rule here tells it to fire 10 minutes before SPY's market close
            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.BeforeMarketClose("SPY", 10), this.EveryDayAfterMarketClose);
            // schedule an event to fire on a single day of the week
            this.Schedule.On(this.DateRules.Every(DayOfWeek.Wednesday), this.TimeRules.At(12, 0), this.EveryWedAtNoon);
            // schedule an event to fire on certain days of the week
            this.Schedule.On(this.DateRules.Every(DayOfWeek.Monday, DayOfWeek.Friday), this.TimeRules.At(12, 0), this.EveryMonFriAtNoon);
            // the scheduling methods return the ScheduledEvent object which can be used for other things here I set
            // the event up to check the portfolio value every 10 minutes, and liquidate if we have too many losses
            this.Schedule.On(this.DateRules.EveryDay(), this.TimeRules.Every(new timedelta(minutes: 10)), this.LiquidateUnrealizedLosses);
            // schedule an event to fire at the beginning of the month, the symbol is optional
            // if specified, it will fire the first trading day for that symbol of the month,
            // if not specified it will fire on the first day of the month
            this.Schedule.On(this.DateRules.MonthStart("SPY"), this.TimeRules.AfterMarketOpen("SPY"), this.RebalancingCode);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 1);
            }
        }
        
        public virtual object SpecificTime() {
            this.Log("SpecificTime: Fired at : {self.Time}");
        }
        
        public virtual object EveryDayAfterMarketOpen() {
            this.Log("EveryDay.SPY 10 min after open: Fired at: {self.Time}");
        }
        
        public virtual object EveryDayAfterMarketClose() {
            this.Log("EveryDay.SPY 10 min before close: Fired at: {self.Time}");
        }
        
        public virtual object EveryWedAtNoon() {
            this.Log("Wed at 12pm: Fired at: {self.Time}");
        }
        
        public virtual object EveryMonFriAtNoon() {
            this.Log("Mon/Fri at 12pm: Fired at: {self.Time}");
        }
        
        //  if we have over 1000 dollars in unrealized losses, liquidate
        public virtual object LiquidateUnrealizedLosses() {
            if (this.Portfolio.TotalUnrealizedProfit < -1000) {
                this.Log("Liquidated due to unrealized losses at: {self.Time}");
                this.Liquidate();
            }
        }
        
        //  Good spot for rebalancing code?
        public virtual object RebalancingCode() {
        }
    }
}
