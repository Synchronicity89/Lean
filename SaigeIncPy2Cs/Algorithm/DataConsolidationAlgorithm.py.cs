
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class DataConsolidationAlgorithm {
    
    static DataConsolidationAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class DataConsolidationAlgorithm
        : QCAlgorithm {
        
        public object @__last;
        
        public bool consolidated45Minute;
        
        public bool consolidatedHour;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(DateTime(2013, 10, 7, 9, 30, 0));
            this.SetEndDate(this.StartDate + new timedelta(60));
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY");
            this.AddForex("EURUSD", Resolution.Hour);
            // define our 30 minute trade bar consolidator. we can
            // access the 30 minute bar from the DataConsolidated events
            var thirtyMinuteConsolidator = TradeBarConsolidator(new timedelta(minutes: 30));
            // attach our event handler. the event handler is a function that will
            // be called each time we produce a new consolidated piece of data.
            thirtyMinuteConsolidator.DataConsolidated += this.ThirtyMinuteBarHandler;
            // this call adds our 30 minute consolidator to
            // the manager to receive updates from the engine
            this.SubscriptionManager.AddConsolidator("SPY", thirtyMinuteConsolidator);
            // here we'll define a slightly more complex consolidator. what we're trying to produce is
            // a 3 day bar. Now we could just use a single TradeBarConsolidator like above and pass in
            // TimeSpan.FromDays(3), but in reality that's not what we want. For time spans of longer than
            // a day we'll get incorrect results around weekends and such. What we really want are tradeable
            // days. So we'll create a daily consolidator, and then wrap it with a 3 count consolidator.
            // first define a one day trade bar -- this produces a consolidated piece of data after a day has passed
            var oneDayConsolidator = TradeBarConsolidator(new timedelta(1));
            // next define our 3 count trade bar -- this produces a consolidated piece of data after it sees 3 pieces of data
            var threeCountConsolidator = TradeBarConsolidator(3);
            // here we combine them to make a new, 3 day trade bar. The SequentialConsolidator allows composition of
            // consolidators. It takes the consolidated output of one consolidator (in this case, the oneDayConsolidator)
            // and pipes it through to the threeCountConsolidator.  His output will be a 3 day bar.
            var three_oneDayBar = SequentialConsolidator(oneDayConsolidator, threeCountConsolidator);
            // attach our handler
            three_oneDayBar.DataConsolidated += this.ThreeDayBarConsolidatedHandler;
            // this call adds our 3 day to the manager to receive updates from the engine
            this.SubscriptionManager.AddConsolidator("SPY", three_oneDayBar);
            // Custom monthly consolidator
            var customMonthlyConsolidator = TradeBarConsolidator(this.CustomMonthly);
            customMonthlyConsolidator.DataConsolidated += this.CustomMonthlyHandler;
            this.SubscriptionManager.AddConsolidator("SPY", customMonthlyConsolidator);
            // API convenience method for easily receiving consolidated data
            this.Consolidate("SPY", new timedelta(minutes: 45), this.FortyFiveMinuteBarHandler);
            this.Consolidate("SPY", Resolution.Hour, this.HourBarHandler);
            this.Consolidate("EURUSD", Resolution.Daily, this.DailyEurUsdBarHandler);
            // API convenience method for easily receiving weekly-consolidated data
            this.Consolidate("SPY", CalendarType.Weekly, this.CalendarTradeBarHandler);
            this.Consolidate("EURUSD", CalendarType.Weekly, this.CalendarQuoteBarHandler);
            // API convenience method for easily receiving monthly-consolidated data
            this.Consolidate("SPY", CalendarType.Monthly, this.CalendarTradeBarHandler);
            this.Consolidate("EURUSD", CalendarType.Monthly, this.CalendarQuoteBarHandler);
            // some securities may have trade and quote data available, so we can choose it based on TickType:
            //self.Consolidate("BTCUSD", Resolution.Hour, TickType.Trade, self.HourBarHandler)   # to get TradeBar
            //self.Consolidate("BTCUSD", Resolution.Hour, TickType.Quote, self.HourBarHandler)   # to get QuoteBar (default)
            this.consolidatedHour = false;
            this.consolidated45Minute = false;
            this.@__last = null;
        }
        
        // We need to declare this method
        public virtual object OnData(object data) {
        }
        
        public virtual object OnEndOfDay() {
            // close up shop each day and reset our 'last' value so we start tomorrow fresh
            this.Liquidate("SPY");
            this.@__last = null;
        }
        
        // This is our event handler for our 30 minute trade bar defined above in Initialize(). So each time the
        //         consolidator produces a new 30 minute bar, this function will be called automatically. The 'sender' parameter
        //          will be the instance of the IDataConsolidator that invoked the event, but you'll almost never need that!
        public virtual object ThirtyMinuteBarHandler(object sender, object consolidated) {
            if (this.@__last != null && consolidated.Close > this.@__last.Close) {
                this.Log("{consolidated.Time} >> SPY >> LONG  >> 100 >> {self.Portfolio['SPY'].Quantity}");
                this.Order("SPY", 100);
            } else if (this.@__last != null && consolidated.Close < this.@__last.Close) {
                this.Log("{consolidated.Time} >> SPY >> SHORT  >> 100 >> {self.Portfolio['SPY'].Quantity}");
                this.Order("SPY", -100);
            }
            this.@__last = consolidated;
        }
        
        //  This is our event handler for our 3 day trade bar defined above in Initialize(). So each time the
        //         consolidator produces a new 3 day bar, this function will be called automatically. The 'sender' parameter
        //         will be the instance of the IDataConsolidator that invoked the event, but you'll almost never need that!
        public virtual object ThreeDayBarConsolidatedHandler(object sender, object consolidated) {
            this.Log("{consolidated.Time} >> Plotting!");
            this.Plot(consolidated.Symbol.Value, "3HourBar", consolidated.Close);
        }
        
        //  This is our event handler for our 45 minute consolidated defined using the Consolidate method
        public virtual object FortyFiveMinuteBarHandler(object consolidated) {
            this.consolidated45Minute = true;
            this.Log("{consolidated.EndTime} >> FortyFiveMinuteBarHandler >> {consolidated.Close}");
        }
        
        // This is our event handler for our one hour consolidated defined using the Consolidate method
        public virtual object HourBarHandler(object consolidated) {
            this.consolidatedHour = true;
            this.Log("{consolidated.EndTime} >> FortyFiveMinuteBarHandler >> {consolidated.Close}");
        }
        
        // This is our event handler for our daily consolidated defined using the Consolidate method
        public virtual object DailyEurUsdBarHandler(object consolidated) {
            this.Log("{consolidated.EndTime} EURUSD Daily consolidated.");
        }
        
        public virtual object CalendarTradeBarHandler(object tradeBar) {
            this.Log("{self.Time} :: {tradeBar.Time} {tradeBar.Close}");
        }
        
        public virtual object CalendarQuoteBarHandler(object quoteBar) {
            this.Log("{self.Time} :: {quoteBar.Time} {quoteBar.Close}");
        }
        
        // Custom Monthly Func
        public virtual object CustomMonthly(object dt) {
            var start = dt.replace(day: 1).date();
            var end = dt.replace(day: 28) + new timedelta(4);
            end = (end - new timedelta((end.day - 1))).date();
            return CalendarInfo(start, end - start);
        }
        
        // This is our event handler Custom Monthly function
        public virtual object CustomMonthlyHandler(object sender, object consolidated) {
            this.Log("{consolidated.Time} >> CustomMonthlyHandler >> {consolidated.Close}");
        }
        
        public virtual object OnEndOfAlgorithm() {
            if (!this.consolidatedHour) {
                throw new Exception("Expected hourly consolidator to be fired.");
            }
            if (!this.consolidated45Minute) {
                throw new Exception("Expected 45-minute consolidator to be fired.");
            }
        }
    }
}
