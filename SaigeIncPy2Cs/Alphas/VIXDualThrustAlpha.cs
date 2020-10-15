namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using TradeBarConsolidator = QuantConnect.Data.Consolidators.TradeBarConsolidator;
    
    using TradeBar = QuantConnect.Data.Market.TradeBar;
    
    using RollingWindow = QuantConnect.Indicators.RollingWindow;
    
    using BrokerageName = QuantConnect.Brokerages.BrokerageName;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using ManualUniverseSelectionModel = QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel;
    
    using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;
    
    using ImmediateExecutionModel = QuantConnect.Algorithm.Framework.Execution.ImmediateExecutionModel;
    
    using MaximumDrawdownPercentPerSecurity = QuantConnect.Algorithm.Framework.Risk.MaximumDrawdownPercentPerSecurity;
    
    using timedelta = datetime.timedelta;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class VIXDualThrustAlpha {
        
        static VIXDualThrustAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Indicators");
            AddReference("QuantConnect.Algorithm.Framework");
        }
        
        public class VIXDualThrustAlpha
            : QCAlgorithm {
            
            public int consolidatorBars;
            
            public double k1;
            
            public double k2;
            
            public int rangePeriod;
            
            public virtual object Initialize() {
                // -- STRATEGY INPUT PARAMETERS --
                this.k1 = 0.63;
                this.k2 = 0.63;
                this.rangePeriod = 20;
                this.consolidatorBars = 30;
                // Settings
                this.SetStartDate(2018, 10, 1);
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                this.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
                // Universe Selection
                this.UniverseSettings.Resolution = Resolution.Minute;
                var symbols = new List<object> {
                    Symbol.Create("SPY", SecurityType.Equity, Market.USA)
                };
                this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
                // Warming up
                var resolutionInTimeSpan = Extensions.ToTimeSpan(this.UniverseSettings.Resolution);
                var warmUpTimeSpan = Time.Multiply(resolutionInTimeSpan, this.consolidatorBars);
                this.SetWarmUp(warmUpTimeSpan);
                // Alpha Model
                this.SetAlpha(new DualThrustAlphaModel(this.k1, this.k2, this.rangePeriod, this.UniverseSettings.Resolution, this.consolidatorBars));
                //# Portfolio Construction
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                //# Execution
                this.SetExecution(ImmediateExecutionModel());
                //# Risk Management
                this.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.03));
            }
        }
        
        // Alpha model that uses dual-thrust strategy to create insights
        //     https://medium.com/@FMZ_Quant/dual-thrust-trading-strategy-2cc74101a626
        //     or here:
        //     https://www.quantconnect.com/tutorials/strategy-library/dual-thrust-trading-algorithm
        public class DualThrustAlphaModel
            : AlphaModel {
            
            public object consolidatorTimeSpan;
            
            public object k1;
            
            public object k2;
            
            public timedelta period;
            
            public object rangePeriod;
            
            public dict symbolDataBySymbol;
            
            public DualThrustAlphaModel(
                object k1,
                object k2,
                object rangePeriod,
                object resolution = Resolution.Daily,
                object barsToConsolidate = 1) {
                // coefficient that used to determinte upper and lower borders of a breakout channel
                this.k1 = k1;
                this.k2 = k2;
                // period the range is calculated over
                this.rangePeriod = rangePeriod;
                // initialize with empty dict.
                this.symbolDataBySymbol = new dict();
                // time for bars we make the calculations on
                var resolutionInTimeSpan = Extensions.ToTimeSpan(resolution);
                this.consolidatorTimeSpan = Time.Multiply(resolutionInTimeSpan, barsToConsolidate);
                // in 5 days after emission an insight is to be considered expired
                this.period = new timedelta(5);
            }
            
            public virtual object Update(object algorithm, object data) {
                object insightCloseTimeUtc;
                var insights = new List<object>();
                foreach (var _tup_1 in this.symbolDataBySymbol.items()) {
                    var symbol = _tup_1.Item1;
                    var symbolData = _tup_1.Item2;
                    if (!symbolData.IsReady) {
                        continue;
                    }
                    var holding = algorithm.Portfolio[symbol];
                    var price = algorithm.Securities[symbol].Price;
                    // buying condition
                    // - (1) price is above upper line
                    // - (2) and we are not long. this is a first time we crossed the line lately
                    if (price > symbolData.UpperLine && !holding.IsLong) {
                        insightCloseTimeUtc = algorithm.UtcTime + this.period;
                        insights.append(Insight.Price(symbol, insightCloseTimeUtc, InsightDirection.Up));
                    }
                    // selling condition
                    // - (1) price is lower that lower line
                    // - (2) and we are not short. this is a first time we crossed the line lately
                    if (price < symbolData.LowerLine && !holding.IsShort) {
                        insightCloseTimeUtc = algorithm.UtcTime + this.period;
                        insights.append(Insight.Price(symbol, insightCloseTimeUtc, InsightDirection.Down));
                    }
                }
                return insights;
            }
            
            public virtual object OnSecuritiesChanged(object algorithm, object changes) {
                object symbolData;
                // added
                foreach (var symbol in (from x in changes.AddedSecurities
                    select x.Symbol).ToList()) {
                    if (!this.symbolDataBySymbol.Contains(symbol)) {
                        // add symbol/symbolData pair to collection
                        symbolData = new SymbolData(symbol, this.k1, this.k2, this.rangePeriod, this.consolidatorTimeSpan);
                        this.symbolDataBySymbol[symbol] = symbolData;
                        // register consolidator
                        algorithm.SubscriptionManager.AddConsolidator(symbol, symbolData.GetConsolidator());
                    }
                }
                // removed
                foreach (var symbol in (from x in changes.RemovedSecurities
                    select x.Symbol).ToList()) {
                    symbolData = this.symbolDataBySymbol.pop(symbol, null);
                    if (symbolData == null) {
                        algorithm.Error("Unable to remove data from collection: DualThrustAlphaModel");
                    } else {
                        // unsubscribe consolidator from data updates
                        algorithm.SubscriptionManager.RemoveConsolidator(symbol, symbolData.GetConsolidator());
                    }
                }
            }
            
            // Contains data specific to a symbol required by this model
            public class SymbolData {
                
                public object consolidator;
                
                public int LowerLine;
                
                public object rangeWindow;
                
                public object Symbol;
                
                public int UpperLine;
                
                public SymbolData(
                    object symbol,
                    object k1,
                    object k2,
                    object rangePeriod,
                    object consolidatorResolution) {
                    this.Symbol = symbol;
                    this.rangeWindow = RollingWindow[TradeBar](rangePeriod);
                    this.consolidator = TradeBarConsolidator(consolidatorResolution);
                    Func<object, object, object> onDataConsolidated = (sender,consolidated) => {
                        // add new tradebar to
                        this.rangeWindow.Add(consolidated);
                        if (this.rangeWindow.IsReady) {
                            var hh = max((from x in this.rangeWindow
                                select x.High).ToList());
                            var hc = max((from x in this.rangeWindow
                                select x.Close).ToList());
                            var lc = min((from x in this.rangeWindow
                                select x.Close).ToList());
                            var ll = min((from x in this.rangeWindow
                                select x.Low).ToList());
                            var range = max(new List<int> {
                                hh - lc,
                                hc - ll
                            });
                            this.UpperLine = consolidated.Close + k1 * range;
                            this.LowerLine = consolidated.Close - k2 * range;
                        }
                    };
                    // event fired at new consolidated trade bar
                    this.consolidator.DataConsolidated += onDataConsolidated;
                }
                
                // Returns the interior consolidator
                public virtual object GetConsolidator() {
                    return this.consolidator;
                }
                
                public object IsReady {
                    get {
                        return this.rangeWindow.IsReady;
                    }
                }
            }
        }
    }
}
