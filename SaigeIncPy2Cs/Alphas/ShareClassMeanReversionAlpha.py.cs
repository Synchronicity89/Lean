namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using TradeBar = QuantConnect.Data.Market.TradeBar;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using RollingWindow = QuantConnect.Indicators.RollingWindow;
    
    using SimpleMovingAverage = QuantConnect.Indicators.SimpleMovingAverage;
    
    using timedelta = datetime.timedelta;
    
    using datetime = datetime.datetime;
    
    using np = numpy;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    using System.Collections;
    
    public static class ShareClassMeanReversionAlpha {
        
        static ShareClassMeanReversionAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Indicators");
        }
        
        public class ShareClassMeanReversionAlgorithm
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2019, 1, 1);
                this.SetCash(100000);
                this.SetWarmUp(20);
                //# Setup Universe settings and tickers to be used
                var tickers = new List<string> {
                    "VIA",
                    "VIAB"
                };
                this.UniverseSettings.Resolution = Resolution.Minute;
                var symbols = (from ticker in tickers
                    select Symbol.Create(ticker, SecurityType.Equity, Market.USA)).ToList();
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                //# Set Manual Universe Selection
                this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
                //# Set Custom Alpha Model
                this.SetAlpha(new ShareClassMeanReversionAlphaModel(tickers: tickers));
                //# Set Equal Weighting Portfolio Construction Model
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                //# Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                //# Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        //  Initialize helper variables for the algorithm
        public class ShareClassMeanReversionAlphaModel
            : AlphaModel {
            
            public object alpha;
            
            public object beta;
            
            public double insight_magnitude;
            
            public bool invested;
            
            public string liquidate;
            
            public object long_symbol;
            
            public None position_value;
            
            public object position_window;
            
            public object prediction_interval;
            
            public object resolution;
            
            public object short_symbol;
            
            public object sma;
            
            public object tickers;
            
            public ShareClassMeanReversionAlphaModel(Hashtable kwargs, params object [] args) {
                this.sma = SimpleMovingAverage(10);
                this.position_window = RollingWindow[Decimal](2);
                this.alpha = null;
                this.beta = null;
                if (!kwargs.Contains("tickers")) {
                    throw new Exception("ShareClassMeanReversionAlphaModel: Missing argument: \"tickers\"");
                }
                this.tickers = kwargs["tickers"];
                this.position_value = null;
                this.invested = false;
                this.liquidate = "liquidate";
                this.long_symbol = this.tickers[0];
                this.short_symbol = this.tickers[1];
                this.resolution = kwargs.Contains("resolution") ? kwargs["resolution"] : Resolution.Minute;
                this.prediction_interval = Time.Multiply(Extensions.ToTimeSpan(this.resolution), 5);
                this.insight_magnitude = 0.001;
            }
            
            public virtual object Update(object algorithm, object data) {
                var insights = new List<object>();
                //# Check to see if either ticker will return a NoneBar, and skip the data slice if so
                foreach (var security in algorithm.Securities) {
                    if (this.DataEventOccured(data, security.Key)) {
                        return insights;
                    }
                }
                //# If Alpha and Beta haven't been calculated yet, then do so
                if (this.alpha == null || this.beta == null) {
                    this.CalculateAlphaBeta(algorithm, data);
                    algorithm.Log("Alpha: " + this.alpha.ToString());
                    algorithm.Log("Beta: " + this.beta.ToString());
                }
                //# If the SMA isn't fully warmed up, then perform an update
                if (!this.sma.IsReady) {
                    this.UpdateIndicators(data);
                    return insights;
                }
                //# Update indicator and Rolling Window for each data slice passed into Update() method
                this.UpdateIndicators(data);
                //# Check to see if the portfolio is invested. If no, then perform value comparisons and emit insights accordingly
                if (!this.invested) {
                    if (this.position_value >= this.sma.Current.Value) {
                        insights.append(Insight(this.long_symbol, this.prediction_interval, InsightType.Price, InsightDirection.Down, this.insight_magnitude, null));
                        insights.append(Insight(this.short_symbol, this.prediction_interval, InsightType.Price, InsightDirection.Up, this.insight_magnitude, null));
                        //# Reset invested boolean
                        this.invested = true;
                    } else if (this.position_value < this.sma.Current.Value) {
                        insights.append(Insight(this.long_symbol, this.prediction_interval, InsightType.Price, InsightDirection.Up, this.insight_magnitude, null));
                        insights.append(Insight(this.short_symbol, this.prediction_interval, InsightType.Price, InsightDirection.Down, this.insight_magnitude, null));
                        //# Reset invested boolean
                        this.invested = true;
                    }
                } else if (this.invested && this.CrossedMean()) {
                    //# If the portfolio is invested and crossed back over the SMA, then emit flat insights
                    //# Reset invested boolean
                    this.invested = false;
                }
                return Insight.Group(insights);
            }
            
            public virtual object DataEventOccured(object data, object symbol) {
                //# Helper function to check to see if data slice will contain a symbol
                if (data.Splits.ContainsKey(symbol) || data.Dividends.ContainsKey(symbol) || data.Delistings.ContainsKey(symbol) || data.SymbolChangedEvents.ContainsKey(symbol)) {
                    return true;
                }
            }
            
            public virtual object UpdateIndicators(object data) {
                //# Calculate position value and update the SMA indicator and Rolling Window
                this.position_value = this.alpha * data[this.long_symbol].Close - this.beta * data[this.short_symbol].Close;
                this.sma.Update(data[this.long_symbol].EndTime, this.position_value);
                this.position_window.Add(this.position_value);
            }
            
            public virtual object CrossedMean() {
                //# Check to see if the position value has crossed the SMA and then return a boolean value
                if (this.position_window[0] >= this.sma.Current.Value && this.position_window[1] < this.sma.Current.Value) {
                    return true;
                } else if (this.position_window[0] < this.sma.Current.Value && this.position_window[1] >= this.sma.Current.Value) {
                    return true;
                } else {
                    return false;
                }
            }
            
            public virtual object CalculateAlphaBeta(object algorithm, object data) {
                //# Calculate Alpha and Beta, the initial number of shares for each security needed to achieve a 50/50 weighting
                this.alpha = algorithm.CalculateOrderQuantity(this.long_symbol, 0.5);
                this.beta = algorithm.CalculateOrderQuantity(this.short_symbol, 0.5);
            }
        }
    }
}
