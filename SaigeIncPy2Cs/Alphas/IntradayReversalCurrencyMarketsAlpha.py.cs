namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using ManualUniverseSelectionModel = QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel;
    
    using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;
    
    using datetime = datetime.datetime;
    
    using timedelta = datetime.timedelta;
    
    using time = datetime.time;
    
    using System.Collections.Generic;
    
    public static class IntradayReversalCurrencyMarketsAlpha {
        
        static IntradayReversalCurrencyMarketsAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Indicators");
        }
        
        public class IntradayReversalCurrencyMarketsAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2015, 1, 1);
                this.SetCash(100000);
                // Set zero transaction fees
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                // Select resolution
                var resolution = Resolution.Hour;
                // Reversion on the USD.
                var symbols = new List<object> {
                    Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda)
                };
                // Set requested data resolution
                this.UniverseSettings.Resolution = resolution;
                this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
                this.SetAlpha(new IntradayReversalAlphaModel(5, resolution));
                // Equally weigh securities in portfolio, based on insights
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                // Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                // Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
                //Set WarmUp for Indicators
                this.SetWarmUp(20);
            }
        }
        
        // Alpha model that uses a Price/SMA Crossover to create insights on Hourly Frequency.
        //     Frequency: Hourly data with 5-hour simple moving average.
        //     Strategy:
        //     Reversal strategy that goes Long when price crosses below SMA and Short when price crosses above SMA.
        //     The trading strategy is implemented only between 10AM - 3PM (NY time)
        public class IntradayReversalAlphaModel
            : AlphaModel {
            
            public Dictionary<object, object> cache;
            
            public string Name;
            
            public object period_sma;
            
            public object resolution;
            
            public IntradayReversalAlphaModel(object period_sma = 5, object resolution = Resolution.Hour) {
                this.period_sma = period_sma;
                this.resolution = resolution;
                this.cache = new Dictionary<object, object> {
                };
                this.Name = "IntradayReversalAlphaModel";
            }
            
            public virtual object Update(object algorithm, object data) {
                // Set the time to close all positions at 3PM
                var timeToClose = algorithm.Time.replace(hour: 15, minute: 1, second: 0);
                var insights = new List<object>();
                foreach (var kvp in algorithm.ActiveSecurities) {
                    var symbol = kvp.Key;
                    if (this.ShouldEmitInsight(algorithm, symbol) && this.cache.Contains(symbol)) {
                        var price = kvp.Value.Price;
                        var symbolData = this.cache[symbol];
                        var direction = symbolData.is_uptrend(price) ? InsightDirection.Up : InsightDirection.Down;
                        // Ignore signal for same direction as previous signal (when no crossover)
                        if (direction == symbolData.PreviousDirection) {
                            continue;
                        }
                        // Save the current Insight Direction to check when the crossover happens
                        symbolData.PreviousDirection = direction;
                        // Generate insight
                        insights.append(Insight.Price(symbol, timeToClose, direction));
                    }
                }
                return insights;
            }
            
            // Handle creation of the new security and its cache class.
            //         Simplified in this example as there is 1 asset.
            public virtual object OnSecuritiesChanged(object algorithm, object changes) {
                foreach (var security in changes.AddedSecurities) {
                    this.cache[security.Symbol] = new SymbolData(algorithm, security.Symbol, this.period_sma, this.resolution);
                }
            }
            
            // Time to control when to start and finish emitting (10AM to 3PM)
            public virtual object ShouldEmitInsight(object algorithm, object symbol) {
                var timeOfDay = algorithm.Time.time();
                return algorithm.Securities[symbol].HasData && timeOfDay >= new time(10) && timeOfDay <= new time(15);
            }
        }
        
        public class SymbolData {
            
            public object PreviousDirection;
            
            public object priceSMA;
            
            public SymbolData(object algorithm, object symbol, object period_sma, object resolution) {
                this.PreviousDirection = InsightDirection.Flat;
                this.priceSMA = algorithm.SMA(symbol, period_sma, resolution);
            }
            
            public virtual object is_uptrend(object price) {
                return this.priceSMA.IsReady && price < round(this.priceSMA.Current.Value * 1.001, 6);
            }
        }
    }
}
