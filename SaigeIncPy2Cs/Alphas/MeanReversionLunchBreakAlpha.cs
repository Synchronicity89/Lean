namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;
    
    using CoarseFundamentalUniverseSelectionModel = QuantConnect.Algorithm.Framework.Selection.CoarseFundamentalUniverseSelectionModel;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    using System.Collections;
    
    public static class MeanReversionLunchBreakAlpha {
        
        static MeanReversionLunchBreakAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Indicators");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
        }
        
        public class MeanReversionLunchBreakAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                // Set zero transaction fees
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                // Use Hourly Data For Simplicity
                this.UniverseSettings.Resolution = Resolution.Hour;
                this.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(this.CoarseSelectionFunction));
                // Use MeanReversionLunchBreakAlphaModel to establish insights
                this.SetAlpha(new MeanReversionLunchBreakAlphaModel());
                // Equally weigh securities in portfolio, based on insights
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                // Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                // Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
            
            // Sort the data by daily dollar volume and take the top '20' ETFs
            public virtual object CoarseSelectionFunction(object coarse) {
                var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume).ToList();
                var filtered = (from x in sortedByDollarVolume
                    where !x.HasFundamentalData
                    select x.Symbol).ToList();
                return filtered[::20];
            }
        }
        
        // Uses the price return between the close of previous day to 12:00 the day after to
        //     predict mean-reversion of stock price during lunch break and creates direction prediction
        //     for insights accordingly.
        public class MeanReversionLunchBreakAlphaModel
            : AlphaModel {
            
            public object predictionInterval;
            
            public object resolution;
            
            public dict symbolDataBySymbol;
            
            public MeanReversionLunchBreakAlphaModel(Hashtable kwargs, params object [] args) {
                var lookback = kwargs.Contains("lookback") ? kwargs["lookback"] : 1;
                this.resolution = Resolution.Hour;
                this.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(this.resolution), lookback);
                this.symbolDataBySymbol = new dict();
            }
            
            public virtual object Update(object algorithm, object data) {
                foreach (var _tup_1 in this.symbolDataBySymbol.items()) {
                    var symbol = _tup_1.Item1;
                    var symbolData = _tup_1.Item2;
                    if (data.Bars.ContainsKey(symbol)) {
                        var bar = data.Bars.GetValue(symbol);
                        symbolData.Update(bar.EndTime, bar.Close);
                    }
                }
                return algorithm.Time.hour != 12 ? new List<object>() : (from x in this.symbolDataBySymbol.values()
                    select x.Insight).ToList();
            }
            
            public virtual object OnSecuritiesChanged(object algorithm, object changes) {
                foreach (var security in changes.RemovedSecurities) {
                    this.symbolDataBySymbol.pop(security.Symbol, null);
                }
                // Retrieve price history for all securities in the security universe
                // and update the indicators in the SymbolData object
                var symbols = (from x in changes.AddedSecurities
                    select x.Symbol).ToList();
                var history = algorithm.History(symbols, 1, this.resolution);
                if (history.empty) {
                    algorithm.Debug("No data on {algorithm.Time}");
                    return;
                }
                history = history.close.unstack(level: 0);
                foreach (var _tup_1 in history) {
                    var ticker = _tup_1.Item1;
                    var values = _tup_1.Item2;
                    var symbol = next(from x in symbols
                        where x.ToString() == ticker
                        select x, null);
                    if (this.symbolDataBySymbol.Contains(symbol) || symbol == null) {
                        continue;
                    }
                    this.symbolDataBySymbol[symbol] = new SymbolData(symbol, this.predictionInterval);
                    this.symbolDataBySymbol[symbol].Update(values.index[0], values[0]);
                }
            }
            
            public class SymbolData {
                
                public object meanOfPriceChange;
                
                public object period;
                
                public object priceChange;
                
                public object symbol;
                
                public SymbolData(object symbol, object period) {
                    this.symbol = symbol;
                    this.period = period;
                    // Mean value of returns for magnitude prediction
                    this.meanOfPriceChange = IndicatorExtensions.SMA(RateOfChangePercent(1), 3);
                    // Price change from close price the previous day
                    this.priceChange = RateOfChangePercent(3);
                }
                
                public virtual object Update(object time, object value) {
                    return this.meanOfPriceChange.Update(time, value) && this.priceChange.Update(time, value);
                }
                
                public object Insight {
                    get {
                        var direction = this.priceChange.Current.Value > 0 ? InsightDirection.Down : InsightDirection.Up;
                        var margnitude = abs(this.meanOfPriceChange.Current.Value);
                        return Insight.Price(this.symbol, this.period, direction, margnitude, null);
                    }
                }
            }
        }
    }
}
