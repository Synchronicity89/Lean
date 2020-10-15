
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class ScheduledUniverseSelectionModelRegressionAlgorithm {
    
    static ScheduledUniverseSelectionModelRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Regression algorithm for testing ScheduledUniverseSelectionModel scheduling functions.
    public class ScheduledUniverseSelectionModelRegressionAlgorithm
        : QCAlgorithm {
        
        public List<object> seenDays;
        
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Hour;
            this.SetStartDate(2017, 1, 1);
            this.SetEndDate(2017, 2, 1);
            // selection will run on mon/tues/thurs at 00:00/06:00/12:00/18:00
            this.SetUniverseSelection(ScheduledUniverseSelectionModel(this.DateRules.Every(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday), this.TimeRules.Every(new timedelta(hours: 12)), this.SelectSymbols));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(1)));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            // some days of the week have different behavior the first time -- less securities to remove
            this.seenDays = new List<object>();
        }
        
        public virtual object SelectSymbols(object dateTime) {
            var symbols = new List<object>();
            var weekday = dateTime.weekday();
            if (weekday == 0 || weekday == 1) {
                symbols.append(Symbol.Create("SPY", SecurityType.Equity, Market.USA));
            } else if (weekday == 2) {
                // given the date/time rules specified in Initialize, this symbol will never be selected (not invoked on wednesdays)
                symbols.append(Symbol.Create("AAPL", SecurityType.Equity, Market.USA));
            } else {
                symbols.append(Symbol.Create("IBM", SecurityType.Equity, Market.USA));
            }
            if (weekday == 1 || weekday == 3) {
                symbols.append(Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM));
            } else if (weekday == 4) {
                // given the date/time rules specified in Initialize, this symbol will never be selected (every 6 hours never lands on hour==1)
                symbols.append(Symbol.Create("EURGBP", SecurityType.Forex, Market.FXCM));
            } else {
                symbols.append(Symbol.Create("NZDUSD", SecurityType.Forex, Market.FXCM));
            }
            return symbols;
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            this.Log("{}: {}".format(this.Time, changes));
            var weekday = this.Time.weekday();
            if (weekday == 0) {
                this.ExpectAdditions(changes, "SPY", "NZDUSD");
                if (!this.seenDays.Contains(weekday)) {
                    this.seenDays.append(weekday);
                    this.ExpectRemovals(changes, null);
                } else {
                    this.ExpectRemovals(changes, "EURUSD", "IBM");
                }
            }
            if (weekday == 1) {
                this.ExpectAdditions(changes, "EURUSD");
                if (!this.seenDays.Contains(weekday)) {
                    this.seenDays.append(weekday);
                    this.ExpectRemovals(changes, "NZDUSD");
                } else {
                    this.ExpectRemovals(changes, "NZDUSD");
                }
            }
            if (weekday == 2 || weekday == 4) {
                // selection function not invoked on wednesdays (2) or friday (4)
                this.ExpectAdditions(changes, null);
                this.ExpectRemovals(changes, null);
            }
            if (weekday == 3) {
                this.ExpectAdditions(changes, "IBM");
                this.ExpectRemovals(changes, "SPY");
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log("{}: {}".format(this.Time, orderEvent));
        }
        
        public virtual object ExpectAdditions(object changes, params object [] tickers) {
            if (tickers == null && changes.AddedSecurities.Count > 0) {
                throw new Exception("{}: Expected no additions: {}".format(this.Time, this.Time.weekday()));
            }
            foreach (var ticker in tickers) {
                if (ticker != null && !(from s in changes.AddedSecurities
                    select s.Symbol.Value).ToList().Contains(ticker)) {
                    throw new Exception("{}: Expected {} to be added: {}".format(this.Time, ticker, this.Time.weekday()));
                }
            }
        }
        
        public virtual object ExpectRemovals(object changes, params object [] tickers) {
            if (tickers == null && changes.RemovedSecurities.Count > 0) {
                throw new Exception("{}: Expected no removals: {}".format(this.Time, this.Time.weekday()));
            }
            foreach (var ticker in tickers) {
                if (ticker != null && !(from s in changes.RemovedSecurities
                    select s.Symbol.Value).ToList().Contains(ticker)) {
                    throw new Exception("{}: Expected {} to be removed: {}".format(this.Time, ticker, this.Time.weekday()));
                }
            }
        }
    }
}
