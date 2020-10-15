
using AddReference = clr.AddReference;

using List = System.Collections.Generic.List;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

using System;

public static class ETFGlobalRotationAlgorithm {
    
    static ETFGlobalRotationAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class ETFGlobalRotationAlgorithm
        : QCAlgorithm {
        
        public bool first;
        
        public object LastRotationTime;
        
        public object oneMonthPerformance;
        
        public timedelta RotationInterval;
        
        public List<object> SymbolData;
        
        public object threeMonthPerformance;
        
        public virtual object Initialize() {
            this.SetCash(25000);
            this.SetStartDate(2007, 1, 1);
            this.LastRotationTime = datetime.min;
            this.RotationInterval = new timedelta(days: 30);
            this.first = true;
            // these are the growth symbols we'll rotate through
            var GrowthSymbols = new List<string> {
                "MDY",
                "IEV",
                "EEM",
                "ILF",
                "EPP"
            };
            // these are the safety symbols we go to when things are looking bad for growth
            var SafetySymbols = new List<string> {
                "EDV",
                "SHY"
            };
            // we'll hold some computed data in these guys
            this.SymbolData = new List<object>();
            foreach (var symbol in (new HashSet<object>(GrowthSymbols) | new HashSet<object>(SafetySymbols)).ToList()) {
                this.AddSecurity(SecurityType.Equity, symbol, Resolution.Minute);
                this.oneMonthPerformance = this.MOM(symbol, 30, Resolution.Daily);
                this.threeMonthPerformance = this.MOM(symbol, 90, Resolution.Daily);
                this.SymbolData.append(new List<list> {
                    symbol,
                    this.oneMonthPerformance,
                    this.threeMonthPerformance
                });
            }
        }
        
        public virtual object OnData(object data) {
            // the first time we come through here we'll need to do some things such as allocation
            // and initializing our symbol data
            if (this.first) {
                this.first = false;
                this.LastRotationTime = this.Time;
                return;
            }
            var delta = this.Time - this.LastRotationTime;
            if (delta > this.RotationInterval) {
                this.LastRotationTime = this.Time;
                var orderedObjScores = this.SymbolData.OrderByDescending(x => Score(x[1].Current.Value, x[2].Current.Value).ObjectiveScore()).ToList();
                foreach (var x in orderedObjScores) {
                    this.Log(">>SCORE>>" + x[0] + ">>" + new Score(x[1].Current.Value, x[2].Current.Value).ObjectiveScore().ToString());
                }
                // pick which one is best from growth and safety symbols
                var bestGrowth = orderedObjScores[0];
                if (new Score(bestGrowth[1].Current.Value, bestGrowth[2].Current.Value).ObjectiveScore() > 0) {
                    if (this.Portfolio[bestGrowth[0]].Quantity == 0) {
                        this.Log("PREBUY>>LIQUIDATE>>");
                        this.Liquidate();
                    }
                    this.Log(">>BUY>>" + bestGrowth[0].ToString() + "@" + (100 * bestGrowth[1].Current.Value).ToString());
                    var qty = this.Portfolio.MarginRemaining / this.Securities[bestGrowth[0]].Close;
                    this.MarketOrder(bestGrowth[0], Convert.ToInt32(qty));
                } else {
                    // if no one has a good objective score then let's hold cash this month to be safe
                    this.Log(">>LIQUIDATE>>CASH");
                    this.Liquidate();
                }
            }
        }
    }
    
    public class Score
        : object {
        
        public object oneMonthPerformance;
        
        public object threeMonthPerformance;
        
        public Score(object oneMonthPerformanceValue, object threeMonthPerformanceValue) {
            this.oneMonthPerformance = oneMonthPerformanceValue;
            this.threeMonthPerformance = threeMonthPerformanceValue;
        }
        
        public virtual object ObjectiveScore() {
            var weight1 = 100;
            var weight2 = 75;
            return (weight1 * this.oneMonthPerformance + weight2 * this.threeMonthPerformance) / (weight1 + weight2);
        }
    }
}
