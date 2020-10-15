namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;
    
    using timedelta = datetime.timedelta;
    
    using datetime = datetime.datetime;
    
    using Decimal = @decimal.Decimal;
    
    using System.Collections.Generic;
    
    public static class RebalancingLeveragedETFAlpha {
        
        static RebalancingLeveragedETFAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
        }
        
        //  Alpha Streams: Benchmark Alpha: Leveraged ETF Rebalancing
        //         Strategy by Prof. Shum, reposted by Ernie Chan.
        //         Source: http://epchan.blogspot.com/2012/10/a-leveraged-etfs-strategy.html
        public class RebalancingLeveragedETFAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2017, 6, 1);
                this.SetEndDate(2018, 8, 1);
                this.SetCash(100000);
                var underlying = new List<string> {
                    "SPY",
                    "QLD",
                    "DIA",
                    "IJR",
                    "MDY",
                    "IWM",
                    "QQQ",
                    "IYE",
                    "EEM",
                    "IYW",
                    "EFA",
                    "GAZB",
                    "SLV",
                    "IEF",
                    "IYM",
                    "IYF",
                    "IYH",
                    "IYR",
                    "IYC",
                    "IBB",
                    "FEZ",
                    "USO",
                    "TLT"
                };
                var ultraLong = new List<string> {
                    "SSO",
                    "UGL",
                    "DDM",
                    "SAA",
                    "MZZ",
                    "UWM",
                    "QLD",
                    "DIG",
                    "EET",
                    "ROM",
                    "EFO",
                    "BOIL",
                    "AGQ",
                    "UST",
                    "UYM",
                    "UYG",
                    "RXL",
                    "URE",
                    "UCC",
                    "BIB",
                    "ULE",
                    "UCO",
                    "UBT"
                };
                var ultraShort = new List<string> {
                    "SDS",
                    "GLL",
                    "DXD",
                    "SDD",
                    "MVV",
                    "TWM",
                    "QID",
                    "DUG",
                    "EEV",
                    "REW",
                    "EFU",
                    "KOLD",
                    "ZSL",
                    "PST",
                    "SMN",
                    "SKF",
                    "RXD",
                    "SRS",
                    "SCC",
                    "BIS",
                    "EPV",
                    "SCO",
                    "TBT"
                };
                var groups = new List<object>();
                foreach (var i in range(underlying.Count)) {
                    var group = new ETFGroup(this.AddEquity(underlying[i], Resolution.Minute).Symbol, this.AddEquity(ultraLong[i], Resolution.Minute).Symbol, this.AddEquity(ultraShort[i], Resolution.Minute).Symbol);
                    groups.append(group);
                }
                // Manually curated universe
                this.SetUniverseSelection(ManualUniverseSelectionModel());
                // Select the demonstration alpha model
                this.SetAlpha(new RebalancingLeveragedETFAlphaModel(groups));
                // Equally weigh securities in portfolio, based on insights
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                // Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                // Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        // 
        //         If the underlying ETF has experienced a return >= 1% since the previous day's close up to the current time at 14:15,
        //         then buy it's ultra ETF right away, and exit at the close. If the return is <= -1%, sell it's ultra-short ETF.
        //     
        public class RebalancingLeveragedETFAlphaModel
            : AlphaModel {
            
            public object date;
            
            public object ETFgroups;
            
            public string Name;
            
            public RebalancingLeveragedETFAlphaModel(object ETFgroups) {
                this.ETFgroups = ETFgroups;
                this.date = datetime.min.date;
                this.Name = "RebalancingLeveragedETFAlphaModel";
            }
            
            // Scan to see if the returns are greater than 1% at 2.15pm to emit an insight.
            public virtual object Update(object algorithm, object data) {
                var insights = new List<object>();
                var magnitude = 0.0005;
                // Paper suggests leveraged ETF's rebalance from 2.15pm - to close
                // giving an insight period of 105 minutes.
                var period = new timedelta(minutes: 105);
                // Get yesterday's close price at the market open
                if (algorithm.Time.date() != this.date) {
                    this.date = algorithm.Time.date();
                    // Save yesterday's price and reset the signal
                    foreach (var group in this.ETFgroups) {
                        var history = algorithm.History(new List<object> {
                            group.underlying
                        }, 1, Resolution.Daily);
                        group.yesterdayClose = history.empty ? null : Decimal(history.loc[group.underlying.ToString()]["close"][0]);
                    }
                }
                // Check if the returns are > 1% at 14.15
                if (algorithm.Time.hour == 14 && algorithm.Time.minute == 15) {
                    foreach (var group in this.ETFgroups) {
                        if (group.yesterdayClose == 0 || group.yesterdayClose == null) {
                            continue;
                        }
                        var returns = round((algorithm.Portfolio[group.underlying].Price - group.yesterdayClose) / group.yesterdayClose, 10);
                        if (returns > 0.01) {
                            insights.append(Insight.Price(group.ultraLong, period, InsightDirection.Up, magnitude));
                        } else if (returns < -0.01) {
                            insights.append(Insight.Price(group.ultraShort, period, InsightDirection.Down, magnitude));
                        }
                    }
                }
                return insights;
            }
        }
        
        // 
        //     Group the underlying ETF and it's ultra ETFs
        //     Args:
        //         underlying: The underlying index ETF
        //         ultraLong: The long-leveraged version of underlying ETF
        //         ultraShort: The short-leveraged version of the underlying ETF
        //     
        public class ETFGroup {
            
            public object ultraLong;
            
            public object ultraShort;
            
            public object underlying;
            
            public int yesterdayClose;
            
            public ETFGroup(object underlying, object ultraLong, object ultraShort) {
                this.underlying = underlying;
                this.ultraLong = ultraLong;
                this.ultraShort = ultraShort;
                this.yesterdayClose = 0;
            }
        }
    }
}
