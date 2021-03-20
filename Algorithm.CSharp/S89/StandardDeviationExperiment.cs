
//namespace QuantConnect.Algorithm.CSharp
//{
//    using QuantConnect.Algorithm.Framework.Alphas;
//    using QuantConnect.Algorithm.Framework.Execution;
//    using QuantConnect.Algorithm.Framework.Portfolio;
//    using QuantConnect.Algorithm.Framework.Risk;
//    using QuantConnect.Algorithm.Framework.Selection;
//    using QuantConnect.Brokerages;
//    using QuantConnect.Data;
//    using QuantConnect.Indicators;
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Text;
//    using System.Threading.Tasks;
//    public class StandardDeviationExperiment : QCAlgorithm
//    {
//        private object lockObject = new object();
//        private List<string> tickers = new List<string> { "SJX", "SKJ", "SKX", "SNQ", "SOV", "SPJ", "STY", "SVX", "SWK", "SXC", "SXM", "SYX", "TAL", "TDY", "TER" };
//        private List<Insight> allInsights = new List<Insight>();
//        public override void Initialize()
//        {
//            // Set Start Date so that backtest has 5+ years of data
//            //SetStartDate(1991, 1, 1);
//            //SetStartDate(2019, 09, 28);
//            SetStartDate(2007, 1, 1);
//            SetStartDate(2017, 4, 28);

//            // No need to set End Date as the final submission will be tested
//            // up until the review date

//            // Set $1m Strategy Cash to trade significant AUM
//            SetCash(1000000);

//            // Add a relevant benchmark, with the default being SPY
//            //tickers.ForEach(t => AddEquity(t));
//            SetBenchmark("SJX");

//            // Use the Alpha Streams Brokerage Model, developed in conjunction with
//            // funds to model their actual fees, costs, etc.
//            // Please do not add any additional reality modelling, such as Slippage, Fees, Buying Power, etc.
//            SetBrokerageModel(new AlphaStreamsBrokerageModel());

//            SetAlpha(new CompositeAlphaModelS89(
//            //new EmaCrossAlphaModel(),
//            //AddAlpha(new EquityHighLowAlphaModel(this));

//            //AddAlpha(new InvestorSentimentSurveyAlphaModel(this));

//            // AddAlpha(new CboeVixAlphaModel(this));

//            // AddAlpha(new CorporateDebtAlphaModel(this));

//            //AddAlpha(new CostOfLivingAlphaModel(this));

//            // AddAlpha(new FederalInterestRateAlphaModel(this));

//            new HistoricalReturnsAlphaModel(14, Resolution.Daily),

//            //AddAlpha(new InflationRateAlphaModel(this));

//            new MacdAlphaModel(12, 26, 9, MovingAverageType.Simple, Resolution.Daily),

//            // AddAlpha(new MiseryIndexAlphaModel(this));

//            // AddAlpha(new MortgageRateVolatilityAlphaModel(this));

//            // AddAlpha(new ProducerPriceIndexAlphaModel(this));

//            // AddAlpha(new PublicDebtAlphaModel(this));

//            new RsiAlphaModel(60, Resolution.Daily)

//            // AddAlpha(new SmartInsiderAlphaModel());

//            // AddAlpha(new TradingEconomicsAlphaModel(this));

//            // AddAlpha(new TreasuryYieldAlphaModel(this));

//            // AddAlpha(new YieldCurveAlphaModel(this));
//            ));

//            SetExecution(new StandardDeviationExecutionModel(60, 1, Resolution.Daily));

//            //SetPortfolioConstruction(new BlackLittermanOptimizationPortfolioConstructionModel());
//            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

//            SetRiskManagement(new TrailingStopRiskManagementModel(0.01m));
//            UniverseSettings.Resolution = Resolution.Daily;
//            //SetUniverseSelection(new QC500UniverseSelectionModel());
//            SetUniverseSelection(new ManualUniverseSelectionModel(tickers.Select(t => base.AddEquity(t, Resolution.Daily).Symbol)));

//            SetWarmup(60);
//        }

//        private void UpdateInsights(IEnumerable<Insight> insights)
//        {
//            lock(lockObject)
//            {
//                allInsights.AddRange(insights.ToArray());
//            }
//        }

//        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
//        /// Slice object keyed by symbol containing the stock data
//        public override void OnData(Slice data)
//        {
//            // if (!Portfolio.Invested)
//            // {
//            //    SetHoldings("SPY", 1);
//            //    Debug("Purchased Stock");
//            // }
//        }

//    }
//}