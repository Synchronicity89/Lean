namespace QuantConnect.Algorithm.CSharp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using QuantConnect;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Brokerages;
    using QuantConnect.Data;
    using QuantConnect.Data.Fundamental;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Interfaces;
    using QuantConnect.Orders;
    using QuantConnect.Securities.Equity;
    using Period = Data.Fundamental.Period;
    using stat = MathNet.Numerics.Statistics.Statistics;

    //import statistics as stat
    //import pickle
    //from collections import deque

    //class DynamicCalibratedGearbox(QCAlgorithm):
    public class G_ScoreInvesting : QCAlgorithm
    {
        private G_ScoreInvesting __this;
        private string tech_ROA_key;
        private int curr_month;

        private Dictionary<Symbol, Queue<decimal>> tech_ROA;
        private int quarters;

        public static class C
        {
            public static readonly CultureInfo en_us = new CultureInfo("en-us");
        }
        //    def Initialize(self):
        public override void Initialize()
        {
            __this = this;
            //        ### IMPORTANT: FOR USERS RUNNING THIS ALGORITHM IN LIVE TRADING,
            //        ### RUN THE BACKTEST ONCE

            //self.tech_ROA_key = 'TECH_ROA'
            __this.tech_ROA_key = "TECH_ROA_CS";
            // we need 3 extra years to warmup our ROA values
            //self.SetStartDate(2012, 9, 1)
            __this.SetStartDate(2012, 9, 1);
            //self.SetEndDate(2020, 9, 1)
            __this.SetEndDate(2019, 9, 1);


            //self.SetCash(100000)  # Set Strategy Cash
            __this.SetCash(100000);

            //self.SetBrokerageModel(    AlphaStreamsBrokerageModel())
            __this.SetBrokerageModel(new AlphaStreamsBrokerageModel());
            //self.SetAlpha(    ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(days = 31)))
            __this.SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(31)));
            //self.SetExecution(    ImmediateExecutionModel())
            //__this.SetExecution(new ImmediateExecutionModel());
            //Be more picky about execution:
            __this.SetExecution(new StandardDeviationExecutionModel(30, 0.25m, Resolution.Daily));
            //self.SetPortfolioConstruction(    EqualWeightingPortfolioConstructionModel(lambda time: None))
            __this.SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(time => null));

            //Not in the original example:
            __this.SetRiskManagement(new TrailingStopRiskManagementModel(0.45m));


            //self.AddUniverseSelection(
            __this.AddUniverseSelection(
            //      FineFundamentalUniverseSelectionModel(self.CoarseFilter, self.FineFilter)
                new FineFundamentalUniverseSelectionModel(this.CoarseFilter, this.FineFilter)
            //)
            );
            //self.UniverseSettings.Resolution = Resolution.Daily
            __this.UniverseSettings.Resolution = Resolution.Daily;

            //self.curr_month = -1
            __this.curr_month = -1;

            //store ROA of tech stocks
            //self.tech_ROA = { }
            __this.tech_ROA = new Dictionary<Symbol, Queue<decimal>>();

            //self.symbols = None
            object symbols = null;

            //        if self.LiveMode and not self.ObjectStore.ContainsKey(self.tech_ROA_key):
            if (LiveMode && !ObjectStore.ContainsKey(tech_ROA_key))
            {
                //self.Quit('QUITTING: USING LIVE MOVE WITHOUT TECH_ROA VALUES IN OBJECT STORE')
                __this.Quit("QUITTING: USING LIVE MOVE WITHOUT TECH_ROA VALUES IN OBJECT STORE");
            }

            //self.quarters = 0
            __this.quarters = 0;
            //ObjectStore.Delete(tech_ROA_key);
#if DEBUG
            //If error happened in testing get rid of bad data.  Comment this out once close to production
            //ObjectStore.Delete(tech_ROA_key);
#endif
        }

        public override void OnEndOfAlgorithm()
        {
            //self.Log('Algorithm End')
            __this.Log("Algorithm End");

            //self.SaveData()
            __this.SaveData();
        }

        //    def    SaveData(self):
        public void SaveData()
        {
            //        '''
            //        Saves the tech ROA data to ObjectStore
            //        '''

            // Symbol objects aren't picklable, hence why we use the ticker string
            //  tech_ROA    = {symbol.Value:ROA for symbol, ROA in self.tech_ROA.items()}
            var tech_ROAsav = new Dictionary<string, Queue<decimal>>();
            foreach (Symbol key in tech_ROA.Keys)
            {
                if (tech_ROAsav.ContainsKey(key.Value))
                {
                    //This probably shouldn't happen, but seemed to be happening so try to handle it anyway
                    tech_ROAsav[key.Value] = this.tech_ROA[key];
                }
                else
                {
                    tech_ROAsav.Add(key.Value, this.tech_ROA[key]);
                }
            }

            //self.ObjectStore.SaveBytes(self.tech_ROA_key, pickle.dumps(tech_ROA))
            //__this.ObjectStore.SaveJson(tech_ROA_key, tech_ROAsav);
        }

        //    def CoarseFilter(self, coarse):
        public IEnumerable<Symbol> CoarseFilter(IEnumerable<CoarseFundamental> coarse)
        {
            //        # load data from ObjectStore
            //        if len(self.tech_ROA) == 0 and self.ObjectStore.ContainsKey(self.tech_ROA_key):
            if (this.tech_ROA.Count == 0 && ObjectStore.ContainsKey(tech_ROA_key))
            {
                //            tech_ROA = self.ObjectStore.ReadBytes(self.tech_ROA_key)
                var tech_ROAstr = ObjectStore.ReadJson<Dictionary<string, Queue<decimal>>>(tech_ROA_key);
                //            tech_ROA = pickle.loads(bytearray(tech_ROA))
                //            self.tech_ROA = { Symbol.Create(ticker, SecurityType.Equity, Market.USA):ROA for ticker, ROA in tech_ROA.items()}
                //Don't have pickle in C# so instead, deserialize a Dictionary with string tickers and convert them into Symbol keys
                this.tech_ROA.Clear();
                foreach (string key in tech_ROAstr.Keys)
                {
                    this.tech_ROA.Add(QuantConnect.Symbol.Create(key, SecurityType.Equity, Market.USA), tech_ROAstr[key]);
                }
                //return list(self.tech_ROA.keys())
                return this.tech_ROA.Keys.ToArray();
            }
            //if self.curr_month == self.Time.month:            
            if (this.curr_month == this.Time.Month)
            {
                //return Universe.Unchanged
                return Universe.Unchanged;
            }
            //self.curr_month = self.Time.month
            __this.curr_month = this.Time.Month;

            // we only want to update our ROA values every three months
            //if self.Time.month % 3 != 1:
            if (this.Time.Month % 3 != 1)
            {
                //return Universe.Unchanged
                return Universe.Unchanged;
            }

            //self.quarters += 1
            __this.quarters += 1;

            //return [c.Symbol for c in coarse if c.HasFundamentalData]
            return coarse.Where(c => c.HasFundamentalData).Select(c => c.Symbol);
        }

        //    def FineFilter(self, fine):
        public IEnumerable<Symbol> FineFilter(IEnumerable<FineFundamental> fine)
        {
            const int maxlen = 12;
            //Debug("Fine count: " + fine.Count());
            //book value == FinancialStatements.BalanceSheet.NetTangibleAssets (book value and NTA are synonyms)
            //BM (Book-to-Market) == book value / MarketCap
            //ROA == OperationRatios.ROA
            //CFROA == FinancialStatements.CashFlowStatement.OperatingCashFlow / FinancialStatements.BalanceSheet.TotalAssets
            //R&D to MktCap == FinancialStatements.IncomeStatement.ResearchAndDevelopment / MarketCap
            //CapEx to MktCap == FinancialStatements.CashFlowStatement.CapExReported / MarketCap
            //Advertising to MktCap == FinancialStatements.IncomeStatement.SellingGeneralAndAdministration / MarketCap
            //  note: this parameter may be slightly higher than pure advertising costs

            //        tech_securities = [f for f in fine if f.AssetClassification.MorningstarSectorCode == MorningstarSectorCode.Technology and
            //                                                f.OperationRatios.ROA.ThreeMonths]
            var tech_securities = fine.Where(f => f.AssetClassification.MorningstarSectorCode == MorningstarSectorCode.Technology &&
                f.OperationRatios.ROA.ThreeMonths != 0.0m);
            //       for security in tech_securities:
            foreach (var security in tech_securities)
            {
                //Debug("Fine security: " + security.Symbol.Value + "; " + security.Value);

                //we use deques instead of RWs since deques are picklable
                //actually in C#, LinkedList would be a good choice, but here we use Queue, though a custom object with its own serialization would be good
                //  symbol = security.Symbol
                var symbol = security.Symbol;
                //if symbol not in self.tech_ROA:
                if (this.tech_ROA.Keys.Contains(symbol) == false)
                {
                    //3 years * 4 quarters = 12 quarters of data
                    //self.tech_ROA[symbol] = deque(maxlen = 12)
                    //note: cannot not set maxlen on LinkedList, but there is a const int defined as 12 so set it on Queue
                    tech_ROA[symbol] = new Queue<decimal>(maxlen);
                }
                //self.tech_ROA [symbol].append(security.OperationRatios.ROA.ThreeMonths)
                this.tech_ROA[symbol].Enqueue(Convert.ToDecimal(security.OperationRatios.ROA.ThreeMonths));
                if (this.tech_ROA[symbol].Count > maxlen) this.tech_ROA[symbol].Dequeue();

            }

            //        if self.LiveMode:
            if (LiveMode)
            {
                //this ensures we don't lose new data from an algorithm outage
                //            self.SaveData()
                SaveData();
            }
            //we want to rebalance in the fourth month after the (fiscal) year ends
            //so that we have the most recent quarter's data
            //if self.Time.month != 4 or(self.quarters < 12 and not self.LiveMode):
            if (Time.Month != 4 || (quarters < 12 && LiveMode == false))
            {
                //return Universe.Unchanged
                return Universe.Unchanged;
            }
            //make sure our stocks has these fundamentals
            //tech_securities = [x for x in tech_securities if x.OperationRatios.ROA.OneYear and
            //                                                        x.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths and
            //                                                        x.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths and
            //                                                        x.FinancialStatements.IncomeStatement.ResearchAndDevelopment.TwelveMonths and
            //                                                        x.FinancialStatements.CashFlowStatement.CapExReported.TwelveMonths and
            //                                                        x.FinancialStatements.IncomeStatement.SellingGeneralAndAdministration.TwelveMonths and
            //                                                        x.MarketCap]
            tech_securities = tech_securities.Where(x => x.OperationRatios.ROA.OneYear != 0.0m &&
                                                                    x.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths != 0.0m &&
                                                                    x.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths != 0.0m &&
                                                                    x.FinancialStatements.IncomeStatement.ResearchAndDevelopment.TwelveMonths != 0.0m &&
                                                                    x.FinancialStatements.CashFlowStatement.CapExReported.TwelveMonths != 0.0m &&
                                                                    x.FinancialStatements.IncomeStatement.SellingGeneralAndAdministration.TwelveMonths != 0.0m &&
                                                                    x.MarketCap != 0.0m);

            //compute the variance of the ROA for each tech stock
            //  tech_VARROA = {symbol:stat.variance(ROA) for symbol, ROA in self.tech_ROA.items() if len(ROA) == ROA.maxlen}
            var tech_VARROA = new Dictionary<Symbol, double>();
            foreach (var key in tech_ROA.Keys)
            {
                var item = tech_ROA[key];
                if (item.Count != maxlen) continue;
                var variance = stat.Variance(item.Select(i => (double)i));
                tech_VARROA[key] = variance;
            }
            //if len(tech_VARROA) < 2:
            if (tech_VARROA.Count < 2)
            {
                //return Universe.Unchanged
                return Universe.Unchanged;
            }
            //  tech_VARROA_median = stat.median(tech_VARROA.values())
            var tech_VARROA_median = stat.Median(tech_VARROA.Values);
            //we will now map tech Symbols to various fundamental ratios, 
            //  and compute the median for each ratio

            //ROA 1-year
            //  tech_ROA1Y = { x.Symbol:x.OperationRatios.ROA.OneYear for x in tech_securities}
            var tech_ROA1Y = MakeDictionary(tech_securities, x => x.OperationRatios.ROA.OneYear);
            //tech_ROA1Y_median = stat.median(tech_ROA1Y.values())
            var tech_ROA1Y_median = stat.Median(tech_ROA1Y.Values.Select(v => (double)v));
            /*
            Cash Flow ROA
            tech_CFROA = {
                x.Symbol: (
                x.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths
                    / x.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths
                    ) for x in tech_securities
            }
            */
            var tech_CFROA = MakeDictionary(tech_securities, x => x.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths
                    / x.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths);
            //  tech_CFROA_median = stat.median(tech_CFROA.values())
            var tech_CFROA_median = stat.Median(tech_CFROA.Values.Select(v => (double)v));
            //R&D to MktCap
            //tech_RD2MktCap = {
            //    x.Symbol: (
            //x.FinancialStatements.IncomeStatement.ResearchAndDevelopment.TwelveMonths / x.MarketCap
            //) for x in tech_securities}
            var tech_RD2MktCap = MakeDictionary(tech_securities, x => x.FinancialStatements.IncomeStatement.ResearchAndDevelopment.TwelveMonths / x.MarketCap);

            //  tech_RD2MktCap_median = stat.median(tech_RD2MktCap.values())
            var tech_RD2MktCap_median = stat.Median(tech_RD2MktCap.Values.Select(v => (double)v));
            //CapEx to MktCap
            //tech_CaPex2MktCap = {
            //    x.Symbol: (
            //x.FinancialStatements.CashFlowStatement.CapExReported.TwelveMonths / x.MarketCap
            //) for x in tech_securities}
            var tech_CaPex2MktCap = MakeDictionary(tech_securities, x => x.FinancialStatements.CashFlowStatement.CapExReported.TwelveMonths / x.MarketCap);
            //  tech_CaPex2MktCap_median = stat.median(tech_CaPex2MktCap.values())
            var tech_CaPex2MktCap_median = stat.Median(tech_CaPex2MktCap.Values.Select(v => (double)v));
            //Advertising to MktCap
            //tech_Ad2MktCap = {
            //    x.Symbol: (
            //x.FinancialStatements.IncomeStatement.SellingGeneralAndAdministration.TwelveMonths / x.MarketCap
            //) for x in tech_securities}
            var tech_Ad2MktCap = MakeDictionary(tech_securities, x => x.FinancialStatements.IncomeStatement.SellingGeneralAndAdministration.TwelveMonths / x.MarketCap);
            //  tech_Ad2MktCap_median = stat.median(tech_Ad2MktCap.values())
            var tech_Ad2MktCap_median = stat.Median(tech_Ad2MktCap.Values.Select(v => (double)v));
            //sort fine by book-to-market ratio, get lower quintile
            //  has_book = [f for f in fine if f.FinancialStatements.BalanceSheet.NetTangibleAssets.TwelveMonths and f.MarketCap]
            var has_book = fine.Where(f => f.FinancialStatements.BalanceSheet.NetTangibleAssets.TwelveMonths != 0.0m && f.MarketCap != 0.0m);
            //  sorted_by_BM = sorted(has_book, key = lambda x: x.FinancialStatements.BalanceSheet.NetTangibleAssets.TwelveMonths / x.MarketCap)[:len(has_book)//4]
            var sorted_by_BM = has_book.OrderBy(x => x.FinancialStatements.BalanceSheet.NetTangibleAssets.TwelveMonths / x.MarketCap).Take((int)Math.Floor(has_book.Count() / 4.0m));
            //choose tech stocks from lower quintile
            //  tech_symbols = [f.Symbol for f in sorted_by_BM if f in tech_securities]
            var tech_symbols = sorted_by_BM.Where(f => tech_securities.Any(ts => ts.Symbol == f.Symbol)).Select(x => x.Symbol);

            //        ratioDicts_medians = [(tech_ROA1Y, tech_ROA1Y_median),
            //                                (tech_CFROA, tech_CFROA_median), (tech_RD2MktCap, tech_RD2MktCap_median),
            //                                (tech_CaPex2MktCap, tech_CaPex2MktCap_median), (tech_Ad2MktCap, tech_Ad2MktCap_median)]
            var ratioDicts_medians = new Dictionary<Dictionary<Symbol, decimal>, double>();
            ratioDicts_medians.Add(tech_ROA1Y, tech_ROA1Y_median);
            ratioDicts_medians.Add(tech_CFROA, tech_CFROA_median); ratioDicts_medians.Add(tech_RD2MktCap, tech_RD2MktCap_median);
            ratioDicts_medians.Add(tech_CaPex2MktCap, tech_CaPex2MktCap_median); ratioDicts_medians.Add(tech_Ad2MktCap, tech_Ad2MktCap_median);

            //def compute_g_score(symbol):
            var compute_g_score = new Func<Symbol, decimal>(symbol =>
            {
                //  g_score = 0
                var g_score = 0.0m;
                //if tech_CFROA[symbol] > tech_ROA1Y[symbol]:
                if (tech_CFROA[symbol] > tech_ROA1Y[symbol])
                {
                    //g_score += 1
                    g_score += 1.0m;
                }
                //if symbol in tech_VARROA          and tech_VARROA[symbol] < tech_VARROA_median:
                if (tech_VARROA.ContainsKey(symbol) && tech_VARROA[symbol] < tech_VARROA_median)
                    //g_score += 1
                    g_score += 1.0m;
                //     for   ratio_dict, median in ratioDicts_medians:
                foreach (var ratio_dict in ratioDicts_medians.Keys)
                {
                    var median = ratioDicts_medians[ratio_dict];
                    //if symbol in ratio_dict          and ratio_dict[symbol] > median:
                    if (ratio_dict.ContainsKey(symbol) && ratio_dict[symbol] > (decimal)median)
                        //g_score += 1
                        g_score += 1;
                }
                //return g_score
                return g_score;
            });
            //compute g-scores for each symbol    
            //  g_scores = { symbol: compute_g_score(symbol) for symbol in tech_symbols}
            var g_scores = new Dictionary<Symbol, decimal>();
            foreach (var symbol in tech_symbols)
            {
                g_scores.Add(symbol, compute_g_score(symbol));
            }

            //return [symbol for symbol, g_score in g_scores.items() if g_score >= 5]
            return g_scores.Keys.Where(key => g_scores[key] >= 5.0m);
        }
        private static Dictionary<Symbol, decimal> MakeDictionary(IEnumerable<FineFundamental> tech_securities, Func<FineFundamental, decimal> dec)
        {
            var tech_dict = new Dictionary<Symbol, decimal>();
            foreach (var x in tech_securities)
            {
                tech_dict.Add(x.Symbol, dec(x));
            }
            return tech_dict;
        }
    }
}