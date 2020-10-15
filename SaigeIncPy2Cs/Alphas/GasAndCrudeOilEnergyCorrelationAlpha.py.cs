namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using OrderStatus = QuantConnect.Orders.OrderStatus;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using pd = pandas;
    
    using timedelta = datetime.timedelta;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    using System;
    
    using System.Collections;
    
    public static class GasAndCrudeOilEnergyCorrelationAlpha {
        
        static GasAndCrudeOilEnergyCorrelationAlpha() {
            @"
    Energy prices, especially Oil and Natural Gas, are in general fairly correlated,
    meaning they typically move in the same direction as an overall trend. This Alpha
    uses this idea and implements an Alpha Model that takes Natural Gas ETF price
    movements as a leading indicator for Crude Oil ETF price movements. We take the
    Natural Gas/Crude Oil ETF pair with the highest historical price correlation and
    then create insights for Crude Oil depending on whether or not the Natural Gas ETF price change
    is above/below a certain threshold that we set (arbitrarily).



    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
";
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Indicators");
            AddReference("QuantConnect.Algorithm.Framework");
        }
        
        public class GasAndCrudeOilEnergyCorrelationAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                var natural_gas = (from x in new List<object> {
                    "UNG",
                    "BOIL",
                    "FCG"
                }
                    select Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
                var crude_oil = (from x in new List<object> {
                    "USO",
                    "UCO",
                    "DBO"
                }
                    select Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
                //# Set Universe Selection
                this.UniverseSettings.Resolution = Resolution.Minute;
                this.SetUniverseSelection(ManualUniverseSelectionModel(natural_gas + crude_oil));
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                //# Custom Alpha Model
                this.SetAlpha(new PairsAlphaModel(leading: natural_gas, following: crude_oil, history_days: 90, resolution: Resolution.Minute));
                //# Equal-weight our positions, in this case 100% in USO
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(resolution: Resolution.Minute));
                //# Immediate Execution Fill Model
                this.SetExecution(new CustomExecutionModel());
                //# Null Risk-Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
            
            public virtual object OnOrderEvent(object orderEvent) {
                if (orderEvent.Status == OrderStatus.Filled) {
                    this.Debug("Purchased Stock: {orderEvent.Symbol}");
                }
            }
            
            public virtual object OnEndOfAlgorithm() {
                foreach (var kvp in this.Portfolio) {
                    if (kvp.Value.Invested) {
                        this.Log("Invested in: {kvp.Key}");
                    }
                }
            }
        }
        
        // This Alpha model assumes that the ETF for natural gas is a good leading-indicator
        //         of the price of the crude oil ETF. The model will take in arguments for a threshold
        //         at which the model triggers an insight, the length of the look-back period for evaluating
        //         rate-of-change of UNG prices, and the duration of the insight
        public class PairsAlphaModel {
            
            public object difference_trigger;
            
            public object following;
            
            public object history_days;
            
            public object leading;
            
            public object lookback;
            
            public timedelta next_update;
            
            public Tuple<object, object> pairs;
            
            public object prediction_interval;
            
            public object resolution;
            
            public Dictionary<object, object> symbolDataBySymbol;
            
            public PairsAlphaModel(Hashtable kwargs, params object [] args) {
                this.leading = kwargs.get("leading", new List<object>());
                this.following = kwargs.get("following", new List<object>());
                this.history_days = kwargs.get("history_days", 90);
                this.lookback = kwargs.get("lookback", 5);
                this.resolution = kwargs.get("resolution", Resolution.Hour);
                this.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(this.resolution), 5);
                this.difference_trigger = kwargs.get("difference_trigger", 0.75);
                this.symbolDataBySymbol = new Dictionary<object, object> {
                };
                this.next_update = null;
            }
            
            public virtual object Update(object algorithm, object data) {
                if (this.next_update == null || algorithm.Time > this.next_update) {
                    this.CorrelationPairsSelection();
                    this.next_update = algorithm.Time + new timedelta(30);
                }
                var magnitude = round(this.pairs[0].Return / 100, 6);
                //# Check if Natural Gas returns are greater than the threshold we've set
                if (this.pairs[0].Return > this.difference_trigger) {
                    return new List<object> {
                        Insight.Price(this.pairs[1].Symbol, this.prediction_interval, InsightDirection.Up, magnitude)
                    };
                }
                if (this.pairs[0].Return < -this.difference_trigger) {
                    return new List<object> {
                        Insight.Price(this.pairs[1].Symbol, this.prediction_interval, InsightDirection.Down, magnitude)
                    };
                }
                return new List<object>();
            }
            
            public virtual object CorrelationPairsSelection() {
                //# Get returns for each natural gas/oil ETF
                var daily_return = new Dictionary<object, object> {
                };
                foreach (var _tup_1 in this.symbolDataBySymbol.items()) {
                    var symbol = _tup_1.Item1;
                    var symbolData = _tup_1.Item2;
                    daily_return[symbol] = symbolData.DailyReturnArray;
                }
                //# Estimate coefficients of different correlation measures
                var tau = pd.DataFrame.from_dict(daily_return).corr(method: "kendall");
                //# Calculate the pair with highest historical correlation
                var max_corr = -1;
                foreach (var x in this.leading) {
                    var df = tau[new List<object> {
                        x
                    }].loc[this.following];
                    var corr = float(df.max());
                    if (corr > max_corr) {
                        this.pairs = Tuple.Create(this.symbolDataBySymbol[x], this.symbolDataBySymbol[df.idxmax()[0]]);
                        max_corr = corr;
                    }
                }
            }
            
            // Event fired each time the we add/remove securities from the data feed
            //         Args:
            //             algorithm: The algorithm instance that experienced the change in securities
            //             changes: The security additions and removals from the algorithm
            public virtual object OnSecuritiesChanged(object algorithm, object changes) {
                object symbol;
                object symbolData;
                foreach (var removed in changes.RemovedSecurities) {
                    symbolData = this.symbolDataBySymbol.pop(removed.Symbol, null);
                    if (symbolData != null) {
                        symbolData.RemoveConsolidators(algorithm);
                    }
                }
                // initialize data for added securities
                var symbols = (from x in changes.AddedSecurities
                    select x.Symbol).ToList();
                var history = algorithm.History(symbols, this.history_days + 1, Resolution.Daily);
                if (history.empty) {
                    return;
                }
                var tickers = history.index.levels[0];
                foreach (var ticker in tickers) {
                    symbol = SymbolCache.GetSymbol(ticker);
                    if (!this.symbolDataBySymbol.Contains(symbol)) {
                        symbolData = new SymbolData(symbol, this.history_days, this.lookback, this.resolution, algorithm);
                        this.symbolDataBySymbol[symbol] = symbolData;
                        symbolData.UpdateDailyRateOfChange(history.loc[ticker]);
                    }
                }
                history = algorithm.History(symbols, this.lookback, this.resolution);
                if (history.empty) {
                    return;
                }
                foreach (var ticker in tickers) {
                    symbol = SymbolCache.GetSymbol(ticker);
                    if (this.symbolDataBySymbol.Contains(symbol)) {
                        this.symbolDataBySymbol[symbol].UpdateRateOfChange(history.loc[ticker]);
                    }
                }
            }
        }
        
        // Contains data specific to a symbol required by this model
        public class SymbolData {
            
            public object consolidator;
            
            public object dailyConsolidator;
            
            public object dailyReturn;
            
            public object dailyReturnHistory;
            
            public object rocp;
            
            public object Symbol;
            
            public SymbolData(
                object symbol,
                object dailyLookback,
                object lookback,
                object resolution,
                object algorithm) {
                this.Symbol = symbol;
                this.dailyReturn = RateOfChangePercent("f{symbol}.DailyROCP({1})", 1);
                this.dailyConsolidator = algorithm.ResolveConsolidator(symbol, Resolution.Daily);
                this.dailyReturnHistory = RollingWindow[IndicatorDataPoint](dailyLookback);
                Func<object, object, object> updatedailyReturnHistory = (s,e) => {
                    this.dailyReturnHistory.Add(e);
                };
                this.dailyReturn.Updated += updatedailyReturnHistory;
                algorithm.RegisterIndicator(symbol, this.dailyReturn, this.dailyConsolidator);
                this.rocp = RateOfChangePercent("{symbol}.ROCP({lookback})", lookback);
                this.consolidator = algorithm.ResolveConsolidator(symbol, resolution);
                algorithm.RegisterIndicator(symbol, this.rocp, this.consolidator);
            }
            
            public virtual object RemoveConsolidators(object algorithm) {
                algorithm.SubscriptionManager.RemoveConsolidator(this.Symbol, this.consolidator);
                algorithm.SubscriptionManager.RemoveConsolidator(this.Symbol, this.dailyConsolidator);
            }
            
            public virtual object UpdateRateOfChange(object history) {
                foreach (var tuple in history.itertuples()) {
                    this.rocp.Update(tuple.Index, tuple.close);
                }
            }
            
            public virtual object UpdateDailyRateOfChange(object history) {
                foreach (var tuple in history.itertuples()) {
                    this.dailyReturn.Update(tuple.Index, tuple.close);
                }
            }
            
            public object Return {
                get {
                    return float(this.rocp.Current.Value);
                }
            }
            
            public object DailyReturnArray {
                get {
                    return pd.Series(this.dailyReturnHistory.ToDictionary(x => x.EndTime, x => x.Value));
                }
            }
            
            public virtual object @__repr__() {
                return "{self.rocp.Name} - {Return}";
            }
        }
        
        // Provides an implementation of IExecutionModel that immediately submits market orders to achieve the desired portfolio targets
        public class CustomExecutionModel
            : ExecutionModel {
            
            public object previous_symbol;
            
            public object targetsCollection;
            
            public CustomExecutionModel() {
                this.targetsCollection = PortfolioTargetCollection();
                this.previous_symbol = null;
            }
            
            // Immediately submits orders for the specified portfolio targets.
            //         Args:
            //             algorithm: The algorithm instance
            //             targets: The portfolio targets to be ordered
            public virtual object Execute(object algorithm, object targets) {
                this.targetsCollection.AddRange(targets);
                foreach (var target in this.targetsCollection.OrderByMarginImpact(algorithm)) {
                    var open_quantity = (from x in algorithm.Transactions.GetOpenOrders(target.Symbol)
                        select x.Quantity).ToList().Sum();
                    var existing = algorithm.Securities[target.Symbol].Holdings.Quantity + open_quantity;
                    var quantity = target.Quantity - existing;
                    //# Liquidate positions in Crude Oil ETF that is no longer part of the highest-correlation pair
                    if (target.Symbol.ToString() != this.previous_symbol.ToString() && this.previous_symbol != null) {
                        algorithm.Liquidate(this.previous_symbol);
                    }
                    if (quantity != 0) {
                        algorithm.MarketOrder(target.Symbol, quantity);
                        this.previous_symbol = target.Symbol;
                    }
                }
                this.targetsCollection.ClearFulfilled(algorithm);
            }
        }
    }
}
