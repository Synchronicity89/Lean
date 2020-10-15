namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class MortgageRateVolatilityAlpha {
        
        static MortgageRateVolatilityAlpha() {
            @"
    This Alpha Model uses Wells Fargo 30-year Fixed Rate Mortgage data from Quandl to 
    generate Insights about the movement of Real Estate ETFs. Mortgage rates can provide information 
    regarding the general price trend of real estate, and ETFs provide good continuous-time instruments 
    to measure the impact against. Volatility in mortgage rates tends to put downward pressure on real 
    estate prices, whereas stable mortgage rates, regardless of true rate, lead to stable or higher real
    estate prices. This Alpha model seeks to take advantage of this correlation by emitting insights
    based on volatility and rate deviation from its historic mean.

    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
";
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
            AddReference("QuantConnect.Indicators");
        }
        
        public class MortgageRateVolatilityAlgorithm
            : QCAlgorithmFramework {
            
            public virtual object Initialize() {
                // Set requested data resolution
                this.SetStartDate(2017, 1, 1);
                this.SetCash(100000);
                this.UniverseSettings.Resolution = Resolution.Daily;
                //# Universe of six liquid real estate ETFs
                var etfs = new List<string> {
                    "VNQ",
                    "REET",
                    "TAO",
                    "FREL",
                    "SRET",
                    "HIPS"
                };
                var symbols = (from etf in etfs
                    select Symbol.Create(etf, SecurityType.Equity, Market.USA)).ToList();
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
                this.SetAlpha(new MortgageRateVolatilityAlphaModel(this));
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                this.SetExecution(ImmediateExecutionModel());
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        public class MortgageRateVolatilityAlphaModel
            : AlphaModel {
            
            public object deviations;
            
            public object indicatorPeriod;
            
            public object insightDuration;
            
            public object insightMagnitude;
            
            public object mortgageRate;
            
            public object mortgageRateSma;
            
            public object mortgageRateStd;
            
            public MortgageRateVolatilityAlphaModel(object algorithm, object indicatorPeriod = 15, object insightMagnitude = 0.005, object deviations = 2) {
                //# Add Quandl data for a Well's Fargo 30-year Fixed Rate mortgage
                this.mortgageRate = algorithm.AddData(QuandlMortgagePriceColumns, "WFC/PR_GOV_30YFIXEDVA_APR").Symbol;
                this.indicatorPeriod = indicatorPeriod;
                this.insightDuration = TimeSpan.FromDays(indicatorPeriod);
                this.insightMagnitude = insightMagnitude;
                this.deviations = deviations;
                //# Add indicators for the mortgage rate -- Standard Deviation and Simple Moving Average
                this.mortgageRateStd = algorithm.STD(this.mortgageRate.Value, indicatorPeriod);
                this.mortgageRateSma = algorithm.SMA(this.mortgageRate.Value, indicatorPeriod);
                //# Use a history call to warm-up the indicators
                this.WarmupIndicators(algorithm);
            }
            
            public virtual object Update(object algorithm, object data) {
                var insights = new List<object>();
                //# Return empty list if data slice doesn't contain monrtgage rate data
                if (!data.Keys.Contains(this.mortgageRate)) {
                    return new List<object>();
                }
                //# Extract current mortgage rate, the current STD indicator value, and current SMA value
                var mortgageRate = data[this.mortgageRate].Value;
                var deviation = this.deviations * this.mortgageRateStd.Current.Value;
                var sma = this.mortgageRateSma.Current.Value;
                //# If volatility in mortgage rates is high, then we emit an Insight to sell
                if (mortgageRate < sma - deviation || mortgageRate > sma + deviation) {
                    //# Emit insights for all securities that are currently in the Universe,
                    //# except for the Quandl Symbol
                    insights = (from security in algorithm.ActiveSecurities.Keys
                        where security != this.mortgageRate
                        select Insight(security, this.insightDuration, InsightType.Price, InsightDirection.Down, this.insightMagnitude, null)).ToList();
                }
                //# If volatility in mortgage rates is low, then we emit an Insight to buy
                if (mortgageRate < sma - deviation / 2 || mortgageRate > sma + deviation / 2) {
                    insights = (from security in algorithm.ActiveSecurities.Keys
                        where security != this.mortgageRate
                        select Insight(security, this.insightDuration, InsightType.Price, InsightDirection.Up, this.insightMagnitude, null)).ToList();
                }
                return insights;
            }
            
            public virtual object WarmupIndicators(object algorithm) {
                //# Make a history call and update the indicators
                var history = algorithm.History(this.mortgageRate, this.indicatorPeriod, Resolution.Daily);
                foreach (var _tup_1 in history.iterrows()) {
                    var index = _tup_1.Item1;
                    var row = _tup_1.Item2;
                    this.mortgageRateStd.Update(index[1], row["value"]);
                    this.mortgageRateSma.Update(index[1], row["value"]);
                }
            }
        }
        
        public class QuandlMortgagePriceColumns
            : PythonQuandl {
            
            public string ValueColumnName;
            
            public QuandlMortgagePriceColumns() {
                //# Rename the Quandl object column to the data we want, which is the 'Value' column
                //# of the CSV that our API call returns
                this.ValueColumnName = "Value";
            }
        }
    }
}
