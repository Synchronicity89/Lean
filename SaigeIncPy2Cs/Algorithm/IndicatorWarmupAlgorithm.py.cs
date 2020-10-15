
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class IndicatorWarmupAlgorithm {
    
    static IndicatorWarmupAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class IndicatorWarmupAlgorithm
        : QCAlgorithm {
        
        public Dictionary<object, object> @__sd;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 8);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(1000000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY");
            this.AddEquity("IBM");
            this.AddEquity("BAC");
            this.AddEquity("GOOG", Resolution.Daily);
            this.AddEquity("GOOGL", Resolution.Daily);
            this.@__sd = new Dictionary<object, object> {
            };
            foreach (var security in this.Securities) {
                this.@__sd[security.Key] = new SymbolData(security.Key, this);
            }
            // we want to warm up our algorithm
            this.SetWarmup(this.SymbolData.RequiredBarsWarmup);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            // we are only using warmup for indicator spooling, so wait for us to be warm then continue
            if (this.IsWarmingUp) {
                return;
            }
            foreach (var sd in this.@__sd.values()) {
                var lastPriceTime = sd.Close.Current.Time;
                if (this.RoundDown(lastPriceTime, sd.Security.SubscriptionDataConfig.Increment)) {
                    sd.Update();
                }
            }
        }
        
        public virtual object OnOrderEvent(object fill) {
            var sd = this.@__sd.get(fill.Symbol, null);
            if (sd != null) {
                sd.OnOrderEvent(fill);
            }
        }
        
        public virtual object RoundDown(object time, object increment) {
            if (increment.days != 0) {
                return time.hour == 0 && time.minute == 0 && time.second == 0;
            } else {
                return time.second == 0;
            }
        }
        
        public class SymbolData {
            
            public object @__algorithm;
            
            public None @__currentStopLoss;
            
            public object ADX;
            
            public object Close;
            
            public object EMA;
            
            public bool IsDowntrend;
            
            public object IsReady;
            
            public bool IsUptrend;
            
            public int LotSize;
            
            public object MACD;
            
            public double PercentGlobalStopLoss;
            
            public double PercentTolerance;
            
            public int RequiredBarsWarmup;
            
            public object Security;
            
            public object Symbol;
            
            public int RequiredBarsWarmup = 40;
            
            public double PercentTolerance = 0.001;
            
            public double PercentGlobalStopLoss = 0.01;
            
            public int LotSize = 10;
            
            public SymbolData(object symbol, object algorithm) {
                this.Symbol = symbol;
                this.@__algorithm = algorithm;
                this.@__currentStopLoss = null;
                this.Security = algorithm.Securities[symbol];
                this.Close = algorithm.Identity(symbol);
                this.ADX = algorithm.ADX(symbol, 14);
                this.EMA = algorithm.EMA(symbol, 14);
                this.MACD = algorithm.MACD(symbol, 12, 26, 9);
                this.IsReady = this.Close.IsReady && this.ADX.IsReady && this.EMA.IsReady && this.MACD.IsReady;
                this.IsUptrend = false;
                this.IsDowntrend = false;
            }
            
            public virtual object Update() {
                this.IsReady = this.Close.IsReady && this.ADX.IsReady && this.EMA.IsReady && this.MACD.IsReady;
                var tolerance = 1 - this.PercentTolerance;
                this.IsUptrend = this.MACD.Signal.Current.Value > this.MACD.Current.Value * tolerance && this.EMA.Current.Value > this.Close.Current.Value * tolerance;
                this.IsDowntrend = this.MACD.Signal.Current.Value < this.MACD.Current.Value * tolerance && this.EMA.Current.Value < this.Close.Current.Value * tolerance;
                this.TryEnter();
                this.TryExit();
            }
            
            public virtual object TryEnter() {
                // can't enter if we're already in
                if (this.Security.Invested) {
                    return false;
                }
                var qty = 0;
                var limit = 0.0;
                if (this.IsUptrend) {
                    // 100 order lots
                    qty = this.LotSize;
                    limit = this.Security.Low;
                } else if (this.IsDowntrend) {
                    qty = -this.LotSize;
                    limit = this.Security.High;
                }
                if (qty != 0) {
                    var ticket = this.@__algorithm.LimitOrder(this.Symbol, qty, limit, "TryEnter at: {0}".format(limit));
                }
            }
            
            public virtual object TryExit() {
                // can't exit if we haven't entered
                if (!this.Security.Invested) {
                    return;
                }
                var limit = 0;
                var qty = this.Security.Holdings.Quantity;
                var exitTolerance = 1 + 2 * this.PercentTolerance;
                if (this.Security.Holdings.IsLong && this.Close.Current.Value * exitTolerance < this.EMA.Current.Value) {
                    limit = this.Security.High;
                } else if (this.Security.Holdings.IsShort && this.Close.Current.Value > this.EMA.Current.Value * exitTolerance) {
                    limit = this.Security.Low;
                }
                if (limit != 0) {
                    var ticket = this.@__algorithm.LimitOrder(this.Symbol, -qty, limit, "TryExit at: {0}".format(limit));
                }
            }
            
            public virtual object OnOrderEvent(object fill) {
                if (fill.Status != OrderStatus.Filled) {
                    return;
                }
                var qty = this.Security.Holdings.Quantity;
                // if we just finished entering, place a stop loss as well
                if (this.Security.Invested) {
                    var stop = this.Security.Holdings.IsLong ? fill.FillPrice * (1 - this.PercentGlobalStopLoss) : fill.FillPrice * (1 + this.PercentGlobalStopLoss);
                    this.@__currentStopLoss = this.@__algorithm.StopMarketOrder(this.Symbol, -qty, stop, "StopLoss at: {0}".format(stop));
                } else if (this.@__currentStopLoss != null && this.@__currentStopLoss.Status != OrderStatus.Filled) {
                    // check for an exit, cancel the stop loss
                    // cancel our current stop loss
                    this.@__currentStopLoss.Cancel("Exited position");
                    this.@__currentStopLoss = null;
                }
            }
        }
    }
}
