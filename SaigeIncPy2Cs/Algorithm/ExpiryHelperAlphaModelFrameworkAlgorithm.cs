
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class ExpiryHelperAlphaModelFrameworkAlgorithm {
    
    static ExpiryHelperAlphaModelFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Expiry Helper framework algorithm uses Expiry helper class in an Alpha Model
    public class ExpiryHelperAlphaModelFrameworkAlgorithm
        : QCAlgorithm {
        
        public object InsightsGenerated;
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Hour;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2014, 1, 1);
            this.SetCash(100000);
            var symbols = new List<object> {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            };
            // set algorithm framework models
            this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
            this.SetAlpha(new ExpiryHelperAlphaModel());
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01));
            this.InsightsGenerated += this.OnInsightsGenerated;
        }
        
        public virtual object OnInsightsGenerated(object s, object e) {
            foreach (var insight in e.Insights) {
                this.Log("{e.DateTimeUtc.isoweekday()}: Close Time {insight.CloseTimeUtc} {insight.CloseTimeUtc.isoweekday()}");
            }
        }
        
        public class ExpiryHelperAlphaModel
            : AlphaModel {
            
            public object direction;
            
            public object nextUpdate;
            
            public None nextUpdate = null;
            
            public object direction = InsightDirection.Up;
            
            public virtual object Update(object algorithm, object data) {
                if (this.nextUpdate != null && this.nextUpdate > algorithm.Time) {
                    return new List<object>();
                }
                var expiry = Expiry.EndOfDay;
                // Use the Expiry helper to calculate a date/time in the future
                this.nextUpdate = expiry(algorithm.Time);
                var weekday = algorithm.Time.isoweekday();
                var insights = new List<object>();
                foreach (var symbol in data.Bars.Keys) {
                    // Expected CloseTime: next month on the same day and time
                    if (weekday == 1) {
                        insights.append(Insight.Price(symbol, Expiry.OneMonth, this.direction));
                    } else if (weekday == 2) {
                        // Expected CloseTime: next month on the 1st at market open time
                        insights.append(Insight.Price(symbol, Expiry.EndOfMonth, this.direction));
                    } else if (weekday == 3) {
                        // Expected CloseTime: next Monday at market open time
                        insights.append(Insight.Price(symbol, Expiry.EndOfWeek, this.direction));
                    } else if (weekday == 4) {
                        // Expected CloseTime: next day (Friday) at market open time
                        insights.append(Insight.Price(symbol, Expiry.EndOfDay, this.direction));
                    }
                }
                return insights;
            }
        }
    }
}
