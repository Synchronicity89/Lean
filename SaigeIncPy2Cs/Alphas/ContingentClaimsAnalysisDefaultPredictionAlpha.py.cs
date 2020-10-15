namespace Alphas {
    
    using AddReference = clr.AddReference;
    
    using sp = scipy.stats;
    
    using pd = pandas;
    
    using np = numpy;
    
    using datetime = datetime.datetime;
    
    using timedelta = datetime.timedelta;
    
    using NullRiskManagementModel = Risk.NullRiskManagementModel.NullRiskManagementModel;
    
    using EqualWeightingPortfolioConstructionModel = Portfolio.EqualWeightingPortfolioConstructionModel.EqualWeightingPortfolioConstructionModel;
    
    using ImmediateExecutionModel = Execution.ImmediateExecutionModel.ImmediateExecutionModel;
    
    using System.Linq;
    
    using System.Collections.Generic;
    
    using System.Collections;
    
    using System;
    
    public static class ContingentClaimsAnalysisDefaultPredictionAlpha {
        
        static ContingentClaimsAnalysisDefaultPredictionAlpha() {
            AddReference("QuantConnect.Algorithm");
        }
        
        //  Contingent Claim Analysis is put forth by Robert Merton, recepient of the Noble Prize in Economics in 1997 for his work in contributing to
        //     Black-Scholes option pricing theory, which says that the equity market value of stockholders’ equity is given by the Black-Scholes solution
        //     for a European call option. This equation takes into account Debt, which in CCA is the equivalent to a strike price in the BS solution. The probability
        //     of default on corporate debt can be calculated as the N(-d2) term, where d2 is a function of the interest rate on debt(µ), face value of the debt (B), value of the firm's assets (V),
        //     standard deviation of the change in a firm's asset value (σ), the dividend and interest payouts due (D), and the time to maturity of the firm's debt(τ). N(*) is the cumulative
        //     distribution function of a standard normal distribution, and calculating N(-d2) gives us the probability of the firm's assets being worth less
        //     than the debt of the company at the time that the debt reaches maturity -- that is, the firm doesn't have enough in assets to pay off its debt and defaults.
        // 
        //     We use a Fine/Coarse Universe Selection model to select small cap stocks, who we postulate are more likely to default
        //     on debt in general than blue-chip companies, and extract Fundamental data to plug into the CCA formula.
        //     This Alpha emits insights based on whether or not a company is likely to default given its probability of default vs a default probability threshold that we set arbitrarily.
        // 
        //     Prob. default (on principal B at maturity T) = Prob(VT < B) = 1 - N(d2) = N(-d2) where -d2(µ) = -{ln(V/B) + [(µ - D) - ½σ2]τ}/ σ √τ.
        //         N(d) = (univariate) cumulative standard normal distribution function (from -inf to d)
        //         B = face value (principal) of the debt
        //         D = dividend + interest payout
        //         V = value of firm’s assets
        //         σ (sigma) = standard deviation of firm value changes (returns in V)
        //         τ (tau)  = time to debt’s maturity
        //         µ (mu) = interest rate
        // 
        //     This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
        //     sourced so the community and client funds can see an example of an alpha.
        public class ContingentClaimsAnalysisDefaultPredictionAlpha
            : QCAlgorithm {
            
            public object month;
            
            public virtual object Initialize() {
                //# Set requested data resolution and variables to help with Universe Selection control
                this.UniverseSettings.Resolution = Resolution.Daily;
                this.month = -1;
                //# Declare single variable to be passed in multiple places -- prevents issue with conflicting start dates declared in different places
                this.SetStartDate(2018, 1, 1);
                this.SetCash(100000);
                //# SPDR Small Cap ETF is a better benchmark than the default SP500
                this.SetBenchmark("IJR");
                //# Set Universe Selection Model
                this.SetUniverseSelection(FineFundamentalUniverseSelectionModel(this.CoarseSelectionFunction, this.FineSelectionFunction, null, null));
                this.SetSecurityInitializer(security => security.SetFeeModel(ConstantFeeModel(0)));
                //# Set CCA Alpha Model
                this.SetAlpha(new ContingentClaimsAnalysisAlphaModel());
                //# Set Portfolio Construction Model
                this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
                //# Set Execution Model
                this.SetExecution(ImmediateExecutionModel());
                //# Set Risk Management Model
                this.SetRiskManagement(NullRiskManagementModel());
            }
            
            public virtual object CoarseSelectionFunction(object coarse) {
                //# Boolean controls so that our symbol universe is only updated once per month
                if (this.Time.month == this.month) {
                    return Universe.Unchanged;
                }
                this.month = this.Time.month;
                //# Sort by dollar volume, lowest to highest
                var sortedByDollarVolume = (from x in coarse
                    where x.HasFundamentalData
                    select x).ToList().OrderByDescending(x => x.DollarVolume).ToList();
                //# Return smallest 750 -- idea is that smaller companies are most likely to go bankrupt than blue-chip companies
                //# Filter for assets with fundamental data
                return (from x in sortedByDollarVolume[::750]
                    select x.Symbol).ToList();
            }
            
            public virtual object FineSelectionFunction(object fine) {
                Func<object, object> IsValid = x => {
                    var statement = x.FinancialStatements;
                    var sheet = statement.BalanceSheet;
                    var total_assets = sheet.TotalAssets;
                    var ratios = x.OperationRatios;
                    return total_assets.OneMonth > 0 && total_assets.ThreeMonths > 0 && total_assets.SixMonths > 0 && total_assets.TwelveMonths > 0 && sheet.CurrentLiabilities.TwelveMonths > 0 && sheet.InterestPayable.TwelveMonths > 0 && ratios.TotalAssetsGrowth.OneYear > 0 && statement.IncomeStatement.GrossDividendPayment.TwelveMonths > 0 && ratios.ROA.OneYear > 0;
                };
                return (from x in fine.OrderBy(x => IsValid(x)).ToList()
                    select x.Symbol).ToList();
            }
        }
        
        public class ContingentClaimsAnalysisAlphaModel {
            
            public double default_threshold;
            
            public Dictionary<object, object> ProbabilityOfDefaultBySymbol;
            
            public ContingentClaimsAnalysisAlphaModel(Hashtable kwargs, params object [] args) {
                this.ProbabilityOfDefaultBySymbol = new Dictionary<object, object> {
                };
                this.default_threshold = kwargs.Contains("default_threshold") ? kwargs["default_threshold"] : 0.25;
            }
            
            // Updates this alpha model with the latest data from the algorithm.
            //         This is called each time the algorithm receives data for subscribed securities
            //         Args:
            //             algorithm: The algorithm instance
            //             data: The new data available
            //         Returns:
            //             The new insights generated
            public virtual object Update(object algorithm, object data) {
                //# Build a list to hold our insights
                var insights = new List<object>();
                foreach (var _tup_1 in this.ProbabilityOfDefaultBySymbol.items()) {
                    var symbol = _tup_1.Item1;
                    var pod = _tup_1.Item2;
                    //# If Prob. of Default is greater than our set threshold, then emit an insight indicating that this asset is trending downward
                    if (pod >= this.default_threshold && pod != 1.0) {
                        insights.append(Insight.Price(symbol, new timedelta(30), InsightDirection.Down, pod, null));
                    }
                }
                return insights;
            }
            
            public virtual object OnSecuritiesChanged(object algorithm, object changes) {
                foreach (var removed in changes.RemovedSecurities) {
                    this.ProbabilityOfDefaultBySymbol.pop(removed.Symbol, null);
                }
                // initialize data for added securities
                var symbols = (from x in changes.AddedSecurities
                    select x.Symbol).ToList();
                foreach (var symbol in symbols) {
                    if (!this.ProbabilityOfDefaultBySymbol.Contains(symbol)) {
                        //# CCA valuation
                        var pod = this.GetProbabilityOfDefault(algorithm, symbol);
                        if (pod != null) {
                            this.ProbabilityOfDefaultBySymbol[symbol] = pod;
                        }
                    }
                }
            }
            
            // This model applies options pricing theory, Black-Scholes specifically,
            //         to fundamental data to give the probability of a default
            public virtual object GetProbabilityOfDefault(object algorithm, object symbol) {
                var security = algorithm.Securities[symbol];
                if (security.Fundamentals == null || security.Fundamentals.FinancialStatements == null || security.Fundamentals.OperationRatios == null) {
                    return null;
                }
                var statement = security.Fundamentals.FinancialStatements;
                var sheet = statement.BalanceSheet;
                var total_assets = sheet.TotalAssets;
                var tau = 360;
                var mu = security.Fundamentals.OperationRatios.ROA.OneYear;
                var V = total_assets.TwelveMonths;
                var B = sheet.CurrentLiabilities.TwelveMonths;
                var D = statement.IncomeStatement.GrossDividendPayment.TwelveMonths + sheet.InterestPayable.TwelveMonths;
                var series = pd.Series(new List<object> {
                    total_assets.OneMonth,
                    total_assets.ThreeMonths,
                    total_assets.SixMonths,
                    V
                });
                var sigma = series.iloc[series.nonzero()[0]];
                sigma = np.std(sigma.pct_change()[1::len(sigma)]);
                var d2 = (np.log(V) - np.log(B) + (mu - D - 0.5 * Math.Pow(sigma, 2.0)) * tau) / (sigma * np.sqrt(tau));
                return sp.norm.cdf(-d2);
            }
        }
    }
}
