namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;
    
    using FundamentalUniverseSelectionModel = Selection.FundamentalUniverseSelectionModel.FundamentalUniverseSelectionModel;
    
    using timedelta = datetime.timedelta;
    
    using datetime = datetime.datetime;
    
    using ceil = math.ceil;
    
    using chain = itertools.chain;
    
    using System.Collections.Generic;
    
    using System.Collections;
    
    using System.Linq;
    
    public static class GreenblattMagicFormulaAlpha {
        
        static GreenblattMagicFormulaAlpha() {
            AddReference("System");
            AddReference("QuantConnect.Common");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Indicators");
            AddReference("QuantConnect.Algorithm.Framework");
        }
        
        //  Alpha Streams: Benchmark Alpha: Pick stocks according to Joel Greenblatt's Magic Formula
        //     This alpha picks stocks according to Joel Greenblatt's Magic Formula.
        //     First, each stock is ranked depending on the relative value of the ratio EV/EBITDA. For example, a stock
        //     that has the lowest EV/EBITDA ratio in the security universe receives a score of one while a stock that has
        //     the tenth lowest EV/EBITDA score would be assigned 10 points.
        // 
        //     Then, each stock is ranked and given a score for the second valuation ratio, Return on Capital (ROC).
        //     Similarly, a stock that has the highest ROC value in the universe gets one score point.
        //     The stocks that receive the lowest combined score are chosen for insights.
        // 
        //     Source: Greenblatt, J. (2010) The Little Book That Beats the Market
        // 
        //     This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
        //     sourced so the community and client funds can see an example of an alpha.
        public class GreenblattMagicFormulaAlpha
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                //Set zero transaction fees
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                // select stocks using MagicFormulaUniverseSelectionModel
                this.SetUniverseSelection(new GreenBlattMagicFormulaUniverseSelectionModel());
                // Use MagicFormulaAlphaModel to establish insights
                this.SetAlpha(new RateOfChangeAlphaModel());
                // Equally weigh securities in portfolio, based on insights
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                //# Set Immediate Execution Model
                this.SetExecution(ImmediateExecutionModel());
                //# Set Null Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
        }
        
        // Uses Rate of Change (ROC) to create magnitude prediction for insights.
        public class RateOfChangeAlphaModel
            : AlphaModel {
            
            public object lookback;
            
            public object predictionInterval;
            
            public object resolution;
            
            public Dictionary<object, object> symbolDataBySymbol;
            
            public RateOfChangeAlphaModel(Hashtable kwargs, params object [] args) {
                this.lookback = kwargs.get("lookback", 1);
                this.resolution = kwargs.get("resolution", Resolution.Daily);
                this.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(this.resolution), this.lookback);
                this.symbolDataBySymbol = new Dictionary<object, object> {
                };
            }
            
            public virtual object Update(object algorithm, object data) {
                var insights = new List<object>();
                foreach (var _tup_1 in this.symbolDataBySymbol.items()) {
                    var symbol = _tup_1.Item1;
                    var symbolData = _tup_1.Item2;
                    if (symbolData.CanEmit) {
                        insights.append(Insight.Price(symbol, this.predictionInterval, InsightDirection.Up, symbolData.Return, null));
                    }
                }
                return insights;
            }
            
            public virtual object OnSecuritiesChanged(object algorithm, object changes) {
                object symbolData;
                // clean up data for removed securities
                foreach (var removed in changes.RemovedSecurities) {
                    symbolData = this.symbolDataBySymbol.pop(removed.Symbol, null);
                    if (symbolData != null) {
                        symbolData.RemoveConsolidators(algorithm);
                    }
                }
                // initialize data for added securities
                var symbols = (from x in changes.AddedSecurities
                    where !this.symbolDataBySymbol.Contains(x.Symbol)
                    select x.Symbol).ToList();
                var history = algorithm.History(symbols, this.lookback, this.resolution);
                if (history.empty) {
                    return;
                }
                foreach (var symbol in symbols) {
                    symbolData = new SymbolData(algorithm, symbol, this.lookback, this.resolution);
                    this.symbolDataBySymbol[symbol] = symbolData;
                    symbolData.WarmUpIndicators(history.loc[symbol]);
                }
            }
        }
        
        // Contains data specific to a symbol required by this model
        public class SymbolData {
            
            public object consolidator;
            
            public object previous;
            
            public object ROC;
            
            public object symbol;
            
            public SymbolData(object algorithm, object symbol, object lookback, object resolution) {
                this.previous = 0;
                this.symbol = symbol;
                this.ROC = RateOfChange("{symbol}.ROC({lookback})", lookback);
                this.consolidator = algorithm.ResolveConsolidator(symbol, resolution);
                algorithm.RegisterIndicator(symbol, this.ROC, this.consolidator);
            }
            
            public virtual object RemoveConsolidators(object algorithm) {
                algorithm.SubscriptionManager.RemoveConsolidator(this.symbol, this.consolidator);
            }
            
            public virtual object WarmUpIndicators(object history) {
                foreach (var tuple in history.itertuples()) {
                    this.ROC.Update(tuple.Index, tuple.close);
                }
            }
            
            public object Return {
                get {
                    return this.ROC.Current.Value;
                }
            }
            
            public object CanEmit {
                get {
                    if (this.previous == this.ROC.Samples) {
                        return false;
                    }
                    this.previous = this.ROC.Samples;
                    return this.ROC.IsReady;
                }
            }
            
            public override object ToString(Hashtable kwargs) {
                return "{self.ROC.Name}: {(1 + self.Return)**252 - 1:.2%}";
            }
        }
        
        // Defines a universe according to Joel Greenblatt's Magic Formula, as a universe selection model for the framework algorithm.
        //        From the universe QC500, stocks are ranked using the valuation ratios, Enterprise Value to EBITDA (EV/EBITDA) and Return on Assets (ROA).
        //     
        public class GreenBlattMagicFormulaUniverseSelectionModel
            : FundamentalUniverseSelectionModel {
            
            public Dictionary<object, object> dollarVolumeBySymbol;
            
            public object lastMonth;
            
            public int NumberOfSymbolsCoarse;
            
            public int NumberOfSymbolsFine;
            
            public int NumberOfSymbolsInPortfolio;
            
            public GreenBlattMagicFormulaUniverseSelectionModel(object filterFineData = true, object universeSettings = null, object securityInitializer = null)
                : base(universeSettings, securityInitializer) {
                // Number of stocks in Coarse Universe
                this.NumberOfSymbolsCoarse = 500;
                // Number of sorted stocks in the fine selection subset using the valuation ratio, EV to EBITDA (EV/EBITDA)
                this.NumberOfSymbolsFine = 20;
                // Final number of stocks in security list, after sorted by the valuation ratio, Return on Assets (ROA)
                this.NumberOfSymbolsInPortfolio = 10;
                this.lastMonth = -1;
                this.dollarVolumeBySymbol = new Dictionary<object, object> {
                };
            }
            
            // Performs coarse selection for constituents.
            //         The stocks must have fundamental data
            public virtual object SelectCoarse(object algorithm, object coarse) {
                var month = algorithm.Time.month;
                if (month == this.lastMonth) {
                    return Universe.Unchanged;
                }
                this.lastMonth = month;
                // sort the stocks by dollar volume and take the top 1000
                var top = (from x in coarse
                    where x.HasFundamentalData
                    select x).ToList().OrderByDescending(x => x.DollarVolume).ToList()[::self.NumberOfSymbolsCoarse];
                this.dollarVolumeBySymbol = top.ToDictionary(i => i.Symbol, i => i.DollarVolume);
                return this.dollarVolumeBySymbol.keys().ToList();
            }
            
            // QC500: Performs fine selection for the coarse selection constituents
            //         The company's headquarter must in the U.S.
            //         The stock must be traded on either the NYSE or NASDAQ
            //         At least half a year since its initial public offering
            //         The stock's market cap must be greater than 500 million
            // 
            //         Magic Formula: Rank stocks by Enterprise Value to EBITDA (EV/EBITDA)
            //         Rank subset of previously ranked stocks (EV/EBITDA), using the valuation ratio Return on Assets (ROA)
            public virtual object SelectFine(object algorithm, object fine) {
                // QC500:
                //# The company's headquarter must in the U.S.
                //# The stock must be traded on either the NYSE or NASDAQ
                //# At least half a year since its initial public offering
                //# The stock's market cap must be greater than 500 million
                var filteredFine = (from x in fine
                    where x.CompanyReference.CountryId == "USA" && (x.CompanyReference.PrimaryExchangeID == "NYS" || x.CompanyReference.PrimaryExchangeID == "NAS") && (algorithm.Time - x.SecurityReference.IPODate).days > 180 && x.EarningReports.BasicAverageShares.ThreeMonths * x.EarningReports.BasicEPS.TwelveMonths * x.ValuationRatios.PERatio > 500000000.0
                    select x).ToList();
                var count = filteredFine.Count;
                if (count == 0) {
                    return new List<object>();
                }
                var myDict = new dict();
                var percent = this.NumberOfSymbolsFine / count;
                // select stocks with top dollar volume in every single sector
                foreach (var key in new List<string> {
                    "N",
                    "M",
                    "U",
                    "T",
                    "B",
                    "I"
                }) {
                    var value = (from x in filteredFine
                        where x.CompanyReference.IndustryTemplateCode == key
                        select x).ToList();
                    value = value.OrderByDescending(x => this.dollarVolumeBySymbol[x.Symbol]).ToList();
                    myDict[key] = value[::ceil((len(value)  *  percent))];
                }
                // stocks in QC500 universe
                var topFine = chain.from_iterable(myDict.values());
                //  Magic Formula:
                //# Rank stocks by Enterprise Value to EBITDA (EV/EBITDA)
                //# Rank subset of previously ranked stocks (EV/EBITDA), using the valuation ratio Return on Assets (ROA)
                // sort stocks in the security universe of QC500 based on Enterprise Value to EBITDA valuation ratio
                var sortedByEVToEBITDA = topFine.OrderByDescending(x => x.ValuationRatios.EVToEBITDA).ToList();
                // sort subset of stocks that have been sorted by Enterprise Value to EBITDA, based on the valuation ratio Return on Assets (ROA)
                var sortedByROA = sortedByEVToEBITDA[::self.NumberOfSymbolsFine].OrderBy(x => x.ValuationRatios.ForwardROA).ToList();
                // retrieve list of securites in portfolio
                return (from f in sortedByROA[::self.NumberOfSymbolsInPortfolio]
                    select f.Symbol).ToList();
            }
        }
    }
}
