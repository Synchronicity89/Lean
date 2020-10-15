namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    using System.Collections.Generic;
    
    public static class HistoryRequestBenchmark {
        
        static HistoryRequestBenchmark() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Indicators");
            AddReference("QuantConnect.Common");
        }
        
        public class HistoryRequestBenchmark
            : QCAlgorithm {
            
            public object symbol;
            
            public virtual object Initialize() {
                this.SetStartDate(2010, 1, 1);
                this.SetEndDate(2018, 1, 1);
                this.SetCash(10000);
                this.symbol = this.AddEquity("SPY").Symbol;
            }
            
            public virtual object OnEndOfDay() {
                var minuteHistory = this.History(new List<object> {
                    this.symbol
                }, 60, Resolution.Minute);
                var lastHourHigh = 0;
                foreach (var _tup_1 in minuteHistory.loc["SPY"].iterrows()) {
                    var index = _tup_1.Item1;
                    var row = _tup_1.Item2;
                    if (lastHourHigh < row["high"]) {
                        lastHourHigh = row["high"];
                    }
                }
                var dailyHistory = this.History(new List<object> {
                    this.symbol
                }, 1, Resolution.Daily).loc["SPY"].head();
                var dailyHistoryHigh = dailyHistory["high"];
                var dailyHistoryLow = dailyHistory["low"];
                var dailyHistoryOpen = dailyHistory["open"];
            }
        }
    }
}
