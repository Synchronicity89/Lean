

namespace QuantConnect.Algorithm.CSharp.S89
{
    using QuantConnect;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Brokerages;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Orders;
    using QuantConnect.Securities;
    using QuantConnect.Securities.Equity;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class BuyAndHoldStocksNoOptions : QCAlgorithm
    {
        Random rand = new Random(25);
        private List<Equity> equities;
        DateTime endDate;
        public override void Initialize()
        {
            SetBenchmark("SPY");
            //Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2019, 8, 7);
            endDate = new DateTime(2021, 8, 7);
            this.SetEndDate(endDate);
            this.SetCash(100000);
            // large cap 33%:
            var largeCap = new List<string> {
                "KPLT", "BBIG", "VICI", "AFRM", "ATER", "DVAX", "SPRT", "LVS", "LCID", "FUTU", "FCEL", "MARA", "MSFT", "HUT", "TMC", "NTNX", "OPAD", "ANY", "CLOV", "IRNT", "ABCL", "HLBZ", "SDC", "VSTM", "PLTR", "APRE", "NIO", "X", "DKNG", "UBER", "QS", "VIPS", "NURO", "SOFI", "MRIN", "PLUG", "ZIVO", "RIOT", "ALT", "SSL", "ALF", "UNG", "FB", "DATS", "BA", "MU", "KSS", "BTBT", "AMTX", "CCL", "MRK", "OPGN", "XENE", "GM", "JSPR", "KOLD", "BABA", "OCGN", "VNE", "XLE", "OXY", "CCXI", "PTGX", "LUV"
                };
            // international 29%
            var international = new List<string>
            {
                //        "SHOP",
                //        "MELI",
                //        "TEAM",
                //        "FVRR",
                //        "SE",
                //        "IQ",
                //        "LULU",
                //        "ASML",
                //        "BILI",
                //        "BIDU",
                //        "NTES",
                //        "TCEHY",
                //        "WIX",
                //        "JD",
                //        "ACN",
                //        "NVCR",
                //        "NXPI",
                //        "CYBR",
                //        "EDU",
                //        "BZUN",
                //        "FTCH",
                //        "CPNG",
                //        "DASTY",
                //        "LOGI",
                //        "NTDOY",
                //        "MOMO",
                //        "ALKS",
                //        "NICE",
                //        "NVO"
            };
            //// Small Cap 19%
            var smallCap = new List<string>
            {
                //        "PRLB",
                //        "BLUE",
                //        "CWH",
                //        "HA",
                //        "OII",
                //        "ANAB",
                //        "CLNE",
                //        "CWBHF",
                //        "DBI",
                //        "SAFT",
                //        "TNC"
            };
            //// Mid Cap 13%
            var midCap = new List<string>
            {
                //        "AYX",
                //        "AXON",
                //        "TTWO",
                //        "SFIX",
                //        "FSLY",
                //        "APPN",
                //        "ZEN",
                //        "UPWK",
                //        "UI",
                //        "NVTA",
                //        "ABMD",
                //        "AVAV",
                //        "AX",
                //        "PLNT",
                //        "TREX",
                //        "FRPT",
                //        "LMND",
                //        "FIVE",
                //        "GDRX",
                //        "CGNX",
                //        "BL",
                //        "MKL",
                //        "IRBT",
                //        "EDIT",
                //        "IPGP",
                //        "GH",
                //        "TRUP"
            };

            equities = (from x in largeCap.Union(smallCap.Union(midCap.Union(international)))
                        select this.AddEquity(x, Resolution.Minute)).ToList();

            this.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            var smallAndMid = smallCap.Union(midCap);
            var securities = (from x in smallAndMid
                              select QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA)).ToList();
            this.SetAlpha(new EmaCrossAlphaModel());
            //this.AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Down, timedelta(days: 10), 0.025, 0.01));
            this.SetExecution(new StandardDeviationExecutionModel(10, 0.25m, Resolution.Minute));
            //self.SetExecution(ImmediateExecutionModel())
            //lambda time: None
            this.SetPortfolioConstruction(new ConfidenceWeightedPortfolioConstructionModel(this.DateRules.MonthStart()));
            //self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.05m));
            this.SetUniverseSelection(new ManualUniverseSelectionModel(securities));
            //taking first 5 for rapid debugging
            var opt = largeCap.Union(international);


            //var optVolume = opt.OrderByDescending(o => o.)



            //var options = (from x in opt
            //               select this.AddOption(x, Resolution.Minute)).ToList();
            //foreach (var option in options)
            //{
            //    var days = RandFromRange(100, 400);
            //    //option.SetFilter(-3, 3, timedelta(days), timedelta(days + 40));
            //    option.SetFilter(OptionFilter);
            //}
        }

        private OptionFilterUniverse OptionFilter(OptionFilterUniverse universe)
        {
            var days = RandFromRange(100, 400);
            //-3, 3, timedelta(days), timedelta(days + 40)
            return universe.Strikes(-3, 3).Expiration(timedelta(days), timedelta(days + 40)).CallsOnly();
        }

        private int RandFromRange(int low, int high)
        {
            if (low >= high)
            {
                throw new ArgumentException("Low is equal or higher than High.");
            }
            var randNum = rand.NextDouble() * (high - low);
            return low + (int)Math.Floor(randNum);
        }

        private TimeSpan timedelta(int days)
        {
            return TimeSpan.FromDays(days);
        }

        //0 to trade, 1 to stop trading and liquidate once, 2 once liquidation complete
        int stopTrading;

        public override void OnData(Slice data)
        {

            //if (this.Time > endDate - TimeSpan.FromDays(14) && stopTrading == 0)
            //{
            //    stopTrading = 1;
            //}

            //if (this.Time > endDate + TimeSpan.FromHours(15.5) && stopTrading == 1)
            //{
            //    this.Liquidate();
            //    //this.Quit("Ending backtest at " + (endDate + TimeSpan.FromHours(15.5)).ToString());
            //    stopTrading = 2;
            //}
            //var option_invested = (from x in this.Portfolio
            //                       where x.Value.Invested && x.Value.Type == SecurityType.Option
            //                       select x.Key).ToList();
            //if (option_invested.Any())
            //{
            //    if (this.Time + timedelta(4) > option_invested[0].ID.Date)
            //    {
            //        this.Liquidate(option_invested[0], "Too close to expiration");
            //    }
            //    return;
            //}
            //foreach (var i in data.OptionChains)
            //{
            //    var chains = i.Value;
            //    if(stopTrading == 0) this.BuyCall(chains);
            //}
        }
        //for testing
        OrderTicket orderTicket;
        public void BuyCall(OptionChain chains)
        {

            var expiry = chains.OrderByDescending(x => x.Expiry).ToList()[0].Expiry;
            var calls = (from i in chains
                         where i.Expiry == expiry && i.Right == OptionRight.Call
                         select i).ToList();
            var call_contracts = calls.OrderBy(x => Math.Abs(x.Strike - x.UnderlyingLastPrice)).ToList();
            if (call_contracts.Count == 0)
            {
                return;
            }
            var call = call_contracts[0];
            if (call.AskPrice == 0.0m) return;
            var quantity = this.Portfolio.TotalPortfolioValue / call.AskPrice;
            quantity = Convert.ToInt32(0.05m * quantity / 100);
            orderTicket = this.Buy(call.Symbol, quantity);

        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = this.Transactions.GetOrderById(orderEvent.OrderId);

            //for testing
            //if (orderTicket != null && orderTicket.OrderId == orderEvent.OrderId && order.LastFillTime != null)
            //{
            //    this.Log("testing LiquidateAnyPositionMatching ...");
            //    LiquidateAnyPositionMatching(order);
            //    if (orderTicket != null && orderTicket.HasOrder) throw new InvalidOperationException("inline test of order Liquidation failed");
            //}
            //end testing

            if (order.Type == OrderType.OptionExercise)
            {
                //liquadate anything with that symbol, test can be uncommented above
                LiquidateAnyPositionMatching(order);
            }
        }

        private void LiquidateAnyPositionMatching(Order order)
        {
            foreach (var position in this.Portfolio)
            {
                if (position.Key.ToString().Contains(order.Symbol.ToString().Split(' ')[0]))
                {
                    this.SetHoldings(position.Key, 0);
                    this.Log("Liquidated " + position.Key.ToString());
                }
            }
        }
    }
}
