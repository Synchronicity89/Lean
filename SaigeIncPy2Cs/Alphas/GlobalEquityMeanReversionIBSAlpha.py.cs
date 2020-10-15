namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;
    
    using ManualUniverseSelectionModel = QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    using System.Collections;
    
    using System;
    
    public static class GlobalEquityMeanReversionIBSAlpha {
        
        static GlobalEquityMeanReversionIBSAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
        }
        
        public class GlobalEquityMeanReversionIBSAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                // Set zero transaction fees
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                // Global Equity ETF tickers
                var tickers = new List<string> {
                    "ECH",
                    "EEM",
                    "EFA",
                    "EPHE",
                    "EPP",
                    "EWA",
                    "EWC",
                    "EWG",
                    "EWH",
                    "EWI",
                    "EWJ",
                    "EWL",
                    "EWM",
                    "EWM",
                    "EWO",
                    "EWP",
                    "EWQ",
                    "EWS",
                    "EWT",
                    "EWU",
                    "EWY",
                    "EWZ",
                    "EZA",
                    "FXI",
                    "GXG",
                    "IDX",
                    "ILF",
                    "EWM",
                    "QQQ",
                    "RSX",
                    "SPY",
                    "THD"
                };
                var symbols = (from ticker in tickers
                    select Symbol.Create(ticker, SecurityType.Equity, Market.USA)).ToList();
                // Manually curated universe
                this.UniverseSettings.Resolution = Resolution.Daily;
                this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
                // Use GlobalEquityMeanReversionAlphaModel to establish insights
                this.SetAlpha(new MeanReversionIBSAlphaModel());
                // Equally weigh securities in portfolio, based on insights
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                // Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                // Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        // Uses ranking of Internal Bar Strength (IBS) to create direction prediction for insights
        public class MeanReversionIBSAlphaModel
            : AlphaModel {
            
            public int numberOfStocks;
            
            public object predictionInterval;
            
            public MeanReversionIBSAlphaModel(Hashtable kwargs, params object [] args) {
                var lookback = kwargs.Contains("lookback") ? kwargs["lookback"] : 1;
                var resolution = kwargs.Contains("resolution") ? kwargs["resolution"] : Resolution.Daily;
                this.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), lookback);
                this.numberOfStocks = kwargs.Contains("numberOfStocks") ? kwargs["numberOfStocks"] : 2;
            }
            
            public virtual object Update(object algorithm, object data) {
                object value;
                object key;
                var insights = new List<object>();
                var symbolsIBS = new dict();
                var returns = new dict();
                foreach (var security in algorithm.ActiveSecurities.Values) {
                    if (security.HasData) {
                        var high = security.High;
                        var low = security.Low;
                        var hilo = high - low;
                        // Do not consider symbol with zero open and avoid division by zero
                        if (security.Open * hilo != 0) {
                            // Internal bar strength (IBS)
                            symbolsIBS[security.Symbol] = (security.Close - low) / hilo;
                            returns[security.Symbol] = security.Close / security.Open - 1;
                        }
                    }
                }
                // Number of stocks cannot be higher than half of symbolsIBS length
                var number_of_stocks = min(Convert.ToInt32(symbolsIBS.Count / 2), this.numberOfStocks);
                if (number_of_stocks == 0) {
                    return new List<object>();
                }
                // Rank securities with the highest IBS value
                var ordered = symbolsIBS.items().OrderByDescending(kv => Tuple.Create(round(kv[1], 6), kv[0])).ToList();
                var highIBS = new dict(ordered[0::number_of_stocks]);
                var lowIBS = new dict(ordered[-number_of_stocks]);
                // Emit "down" insight for the securities with the highest IBS value
                foreach (var _tup_1 in highIBS.items()) {
                    key = _tup_1.Item1;
                    value = _tup_1.Item2;
                    insights.append(Insight.Price(key, this.predictionInterval, InsightDirection.Down, abs(returns[key]), null));
                }
                // Emit "up" insight for the securities with the lowest IBS value
                foreach (var _tup_2 in lowIBS.items()) {
                    key = _tup_2.Item1;
                    value = _tup_2.Item2;
                    insights.append(Insight.Price(key, this.predictionInterval, InsightDirection.Up, abs(returns[key]), null));
                }
                return insights;
            }
        }
    }
}
