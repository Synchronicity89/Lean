namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using TradeBar = QuantConnect.Data.Market.TradeBar;
    
    using PortfolioTarget = QuantConnect.Algorithm.Framework.Portfolio.PortfolioTarget;
    
    using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using ConstantSlippageModel = QuantConnect.Orders.Slippage.ConstantSlippageModel;
    
    using datetime = datetime.datetime;
    
    using timedelta = datetime.timedelta;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class TriangleExchangeRateArbitrageAlpha {
        
        static TriangleExchangeRateArbitrageAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
        }
        
        public class TriangleExchangeRateArbitrageAlgorithm
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2019, 2, 1);
                this.SetCash(100000);
                // Set zero transaction fees
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                //# Select trio of currencies to trade where
                //# Currency A = USD
                //# Currency B = EUR
                //# Currency C = GBP
                var currencies = new List<string> {
                    "EURUSD",
                    "EURGBP",
                    "GBPUSD"
                };
                var symbols = (from currency in currencies
                    select Symbol.Create(currency, SecurityType.Forex, Market.Oanda)).ToList();
                //# Manual universe selection with tick-resolution data
                this.UniverseSettings.Resolution = Resolution.Minute;
                this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
                this.SetAlpha(new ForexTriangleArbitrageAlphaModel(Resolution.Minute, symbols));
                //# Set Equal Weighting Portfolio Construction Model
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                //# Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                //# Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        public class ForexTriangleArbitrageAlphaModel
            : AlphaModel {
            
            public object insight_period;
            
            public object symbols;
            
            public ForexTriangleArbitrageAlphaModel(object insight_resolution, object symbols) {
                this.insight_period = Time.Multiply(Extensions.ToTimeSpan(insight_resolution), 5);
                this.symbols = symbols;
            }
            
            public virtual object Update(object algorithm, object data) {
                //# Check to make sure all currency symbols are present
                if (data.Keys.Count < 3) {
                    return new List<object>();
                }
                //# Extract QuoteBars for all three Forex securities
                var bar_a = data[this.symbols[0]];
                var bar_b = data[this.symbols[1]];
                var bar_c = data[this.symbols[2]];
                //# Calculate the triangle exchange rate
                //# Bid(Currency A -> Currency B) * Bid(Currency B -> Currency C) * Bid(Currency C -> Currency A)
                //# If exchange rates are priced perfectly, then this yield 1. If it is different than 1, then an arbitrage opportunity exists
                var triangleRate = bar_a.Ask.Close / bar_b.Bid.Close / bar_c.Ask.Close;
                //# If the triangle rate is significantly different than 1, then emit insights
                if (triangleRate > 1.0005) {
                    return Insight.Group(new List<object> {
                        Insight.Price(this.symbols[0], this.insight_period, InsightDirection.Up, 0.0001, null),
                        Insight.Price(this.symbols[1], this.insight_period, InsightDirection.Down, 0.0001, null),
                        Insight.Price(this.symbols[2], this.insight_period, InsightDirection.Up, 0.0001, null)
                    });
                }
                return new List<object>();
            }
        }
    }
}
