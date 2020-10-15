namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    using System.Collections;
    
    public static class PriceGapMeanReversionAlpha {
        
        static PriceGapMeanReversionAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Indicators");
        }
        
        // The motivating idea for this Alpha Model is that a large price gap (here we use true outliers --
        //     price gaps that whose absolutely values are greater than 3 * Volatility) is due to rebound
        //     back to an appropriate price or at least retreat from its brief extreme. Using a Coarse Universe selection
        //     function, the algorithm selects the top x-companies by Dollar Volume (x can be any number you choose)
        //     to trade with, and then uses the Standard Deviation of the 100 most-recent closing prices to determine
        //     which price movements are outliers that warrant emitting insights.
        // 
        //     This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
        //     sourced so the community and client funds can see an example of an alpha.
        public class PriceGapMeanReversionAlpha
            : QCAlgorithm {
            
            public object week;
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                //# Initialize variables to be used in controlling frequency of universe selection
                this.week = -1;
                //# Manual Universe Selection
                this.UniverseSettings.Resolution = Resolution.Minute;
                this.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(this.CoarseSelectionFunction));
                //# Set trading fees to $0
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                //# Set custom Alpha Model
                this.SetAlpha(new PriceGapMeanReversionAlphaModel());
                //# Set equal-weighting Portfolio Construction Model
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                //# Set Execution Model
                this.SetExecution(ImmediateExecutionModel());
                //# Set Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
            
            public virtual object CoarseSelectionFunction(object coarse) {
                //# If it isn't a new week, return the same symbols
                var current_week = this.Time.isocalendar()[1];
                if (current_week == this.week) {
                    return Universe.Unchanged;
                }
                this.week = current_week;
                //# If its a new week, then re-filter stocks by Dollar Volume
                var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume).ToList();
                return (from x in sortedByDollarVolume[::25]
                    select x.Symbol).ToList();
            }
        }
        
        public class PriceGapMeanReversionAlphaModel {
            
            public int lookback;
            
            public object prediction_interval;
            
            public object resolution;
            
            public Dictionary<object, object> symbolDataBySymbol;
            
            public PriceGapMeanReversionAlphaModel(Hashtable kwargs, params object [] args) {
                this.lookback = 100;
                this.resolution = kwargs.Contains("resolution") ? kwargs["resolution"] : Resolution.Minute;
                this.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(this.resolution), 5);
                this.symbolDataBySymbol = new Dictionary<object, object> {
                };
            }
            
            public virtual object Update(object algorithm, object data) {
                var insights = new List<object>();
                //# Loop through all Symbol Data objects
                foreach (var _tup_1 in this.symbolDataBySymbol.items()) {
                    var symbol = _tup_1.Item1;
                    var symbolData = _tup_1.Item2;
                    //# Evaluate whether or not the price jump is expected to rebound
                    if (!symbolData.IsTrend(data)) {
                        continue;
                    }
                    //# Emit insights accordingly to the price jump sign
                    var direction = symbolData.PriceJump > 0 ? InsightDirection.Down : InsightDirection.Up;
                    insights.append(Insight.Price(symbol, this.prediction_interval, direction, symbolData.PriceJump, null));
                }
                return insights;
            }
            
            public virtual object OnSecuritiesChanged(object algorithm, object changes) {
                object symbolData;
                // Clean up data for removed securities
                foreach (var removed in changes.RemovedSecurities) {
                    symbolData = this.symbolDataBySymbol.pop(removed.Symbol, null);
                    if (symbolData != null) {
                        symbolData.RemoveConsolidators(algorithm);
                    }
                }
                var symbols = (from x in changes.AddedSecurities
                    where !this.symbolDataBySymbol.Contains(x.Symbol)
                    select x.Symbol).ToList();
                var history = algorithm.History(symbols, this.lookback, this.resolution);
                if (history.empty) {
                    return;
                }
                //# Create and initialize SymbolData objects
                foreach (var symbol in symbols) {
                    symbolData = new SymbolData(algorithm, symbol, this.lookback, this.resolution);
                    symbolData.WarmUpIndicators(history.loc[symbol]);
                    this.symbolDataBySymbol[symbol] = symbolData;
                }
            }
        }
        
        public class SymbolData {
            
            public object close;
            
            public object consolidator;
            
            public object last_price;
            
            public int PriceJump;
            
            public object symbol;
            
            public object volatility;
            
            public SymbolData(object algorithm, object symbol, object lookback, object resolution) {
                this.symbol = symbol;
                this.close = 0;
                this.last_price = 0;
                this.PriceJump = 0;
                this.consolidator = algorithm.ResolveConsolidator(symbol, resolution);
                this.volatility = StandardDeviation("{symbol}.STD({lookback})", lookback);
                algorithm.RegisterIndicator(symbol, this.volatility, this.consolidator);
            }
            
            public virtual object RemoveConsolidators(object algorithm) {
                algorithm.SubscriptionManager.RemoveConsolidator(this.symbol, this.consolidator);
            }
            
            public virtual object WarmUpIndicators(object history) {
                this.close = history.iloc[-1].close;
                foreach (var tuple in history.itertuples()) {
                    this.volatility.Update(tuple.Index, tuple.close);
                }
            }
            
            public virtual object IsTrend(object data) {
                //# Check for any data events that would return a NoneBar in the Alpha Model Update() method
                if (!data.Bars.ContainsKey(this.symbol)) {
                    return false;
                }
                this.last_price = this.close;
                this.close = data.Bars[this.symbol].Close;
                this.PriceJump = this.close / this.last_price - 1;
                return abs(100 * this.PriceJump) > 3 * this.volatility.Current.Value;
            }
        }
    }
}
