namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;
    
    using ManualUniverseSelectionModel = QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel;
    
    using timedelta = datetime.timedelta;
    
    using System.Collections.Generic;
    
    public static class TripleLeverageETFPairVolatilityDecayAlpha {
        
        static TripleLeverageETFPairVolatilityDecayAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
        }
        
        public class TripleLeverageETFPairVolatilityDecayAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                // Set zero transaction fees
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                // 3X ETF pair tickers
                var ultraLong = Symbol.Create("UGLD", SecurityType.Equity, Market.USA);
                var ultraShort = Symbol.Create("DGLD", SecurityType.Equity, Market.USA);
                // Manually curated universe
                this.UniverseSettings.Resolution = Resolution.Daily;
                this.SetUniverseSelection(ManualUniverseSelectionModel(new List<object> {
                    ultraLong,
                    ultraShort
                }));
                // Select the demonstration alpha model
                this.SetAlpha(new RebalancingTripleLeveragedETFAlphaModel(ultraLong, ultraShort));
                //# Set Equal Weighting Portfolio Construction Model
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                //# Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                //# Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        // 
        //         Rebalance a pair of 3x leveraged ETFs and predict that the value of both ETFs in each pair will decrease.
        //     
        public class RebalancingTripleLeveragedETFAlphaModel
            : AlphaModel {
            
            public double magnitude;
            
            public string Name;
            
            public timedelta period;
            
            public object ultraLong;
            
            public object ultraShort;
            
            public RebalancingTripleLeveragedETFAlphaModel(object ultraLong, object ultraShort) {
                // Giving an insight period 1 days.
                this.period = new timedelta(1);
                this.magnitude = 0.001;
                this.ultraLong = ultraLong;
                this.ultraShort = ultraShort;
                this.Name = "RebalancingTripleLeveragedETFAlphaModel";
            }
            
            public virtual object Update(object algorithm, object data) {
                return Insight.Group(new List<object> {
                    Insight.Price(this.ultraLong, this.period, InsightDirection.Down, this.magnitude),
                    Insight.Price(this.ultraShort, this.period, InsightDirection.Down, this.magnitude)
                });
            }
        }
    }
}
