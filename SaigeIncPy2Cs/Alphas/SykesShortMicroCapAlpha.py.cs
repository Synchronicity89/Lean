namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using EqualWeightingPortfolioConstructionModel = QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel;
    
    using FundamentalUniverseSelectionModel = Selection.FundamentalUniverseSelectionModel.FundamentalUniverseSelectionModel;
    
    using System.Collections;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class SykesShortMicroCapAlpha {
        
        static SykesShortMicroCapAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
        }
        
        //  Alpha Streams: Benchmark Alpha: Identify "pumped" penny stocks and predict that the price of a "pumped" penny stock reverts to mean
        // 
        //     This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
        //    sourced so the community and client funds can see an example of an alpha.
        public class SykesShortMicroCapAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                // Set zero transaction fees
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                // select stocks using PennyStockUniverseSelectionModel
                this.UniverseSettings.Resolution = Resolution.Daily;
                this.SetUniverseSelection(new PennyStockUniverseSelectionModel());
                // Use SykesShortMicroCapAlphaModel to establish insights
                this.SetAlpha(new SykesShortMicroCapAlphaModel());
                // Equally weigh securities in portfolio, based on insights
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                // Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                // Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        // Uses ranking of intraday percentage difference between open price and close price to create magnitude and direction prediction for insights
        public class SykesShortMicroCapAlphaModel
            : AlphaModel {
            
            public int numberOfStocks;
            
            public object predictionInterval;
            
            public SykesShortMicroCapAlphaModel(Hashtable kwargs, params object [] args) {
                var lookback = kwargs.Contains("lookback") ? kwargs["lookback"] : 1;
                var resolution = kwargs.Contains("resolution") ? kwargs["resolution"] : Resolution.Daily;
                this.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), lookback);
                this.numberOfStocks = kwargs.Contains("numberOfStocks") ? kwargs["numberOfStocks"] : 10;
            }
            
            public virtual object Update(object algorithm, object data) {
                var insights = new List<object>();
                var symbolsRet = new dict();
                foreach (var security in algorithm.ActiveSecurities.Values) {
                    if (security.HasData) {
                        var open = security.Open;
                        if (open != 0) {
                            // Intraday price change for penny stocks
                            symbolsRet[security.Symbol] = security.Close / open - 1;
                        }
                    }
                }
                // Rank penny stocks on one day price change and retrieve list of ten "pumped" penny stocks
                var pumpedStocks = new dict(symbolsRet.items().OrderBy(kv => Tuple.Create(-round(kv[1], 6), kv[0])).ToList()[::self.numberOfStocks]);
                // Emit "down" insight for "pumped" penny stocks
                foreach (var _tup_1 in pumpedStocks.items()) {
                    var symbol = _tup_1.Item1;
                    var value = _tup_1.Item2;
                    insights.append(Insight.Price(symbol, this.predictionInterval, InsightDirection.Down, abs(value), null));
                }
                return insights;
            }
        }
        
        // Defines a universe of penny stocks, as a universe selection model for the framework algorithm:
        //     The stocks must have fundamental data
        //     The stock must have positive previous-day close price
        //     The stock must have volume between $1000000 and $10000 on the previous trading day
        //     The stock must cost less than $5
        public class PennyStockUniverseSelectionModel
            : FundamentalUniverseSelectionModel {
            
            public object lastMonth;
            
            public int numberOfSymbolsCoarse;
            
            public PennyStockUniverseSelectionModel() {
                // Number of stocks in Coarse Universe
                this.numberOfSymbolsCoarse = 500;
                this.lastMonth = -1;
            }
            
            public virtual object SelectCoarse(object algorithm, object coarse) {
                if (algorithm.Time.month == this.lastMonth) {
                    return Universe.Unchanged;
                }
                this.lastMonth = algorithm.Time.month;
                // sort the stocks by dollar volume and take the top 500
                var top = (from x in coarse
                    where x.HasFundamentalData && 5 > x.Price > 0 && 1000000 > x.Volume > 10000
                    select x).ToList().OrderByDescending(x => x.DollarVolume).ToList()[::self.numberOfSymbolsCoarse];
                return (from x in top
                    select x.Symbol).ToList();
            }
        }
    }
}
