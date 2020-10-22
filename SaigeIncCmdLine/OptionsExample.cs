//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using QuantConnect;
//using QuantConnect.Algorithm;
//using QuantConnect.Data;
//using QuantConnect.Data.Market;
//using QuantConnect.Indicators;
//using QuantConnect.Interfaces;
//using QuantConnect.Orders;
////using QuantConnect.Common;

//namespace SaigeIncCmdLine
//{



//    //### <summary>
//    //### This example demonstrates how to add options for a given underlying equity security.
//    //### It also shows how you can prefilter contracts easily based on strikes and expirations, and how you
//    //### can inspect the option chain to pick a specific option contract to trade.
//    //### </summary>
//    //### <meta name="tag" content="using data" />
//    //### <meta name="tag" content="options" />
//    //### <meta name="tag" content="filter selection" />
//    public class BasicTemplateOptionsAlgorithm : QCAlgorithm
//    {
//        Symbol option_symbol;
//        public BasicTemplateOptionsAlgorithm()
//        {
//            SetStartDate(2016, 1, 1);
//            SetEndDate(2016, 1, 10);
//            SetCash(100000);

//            var option = base.AddOption("GOOG");
//            option_symbol = option.Symbol;
//            //# set our strike/expiry filter for this option chain
//            option.SetFilter(-2, +2, TimeSpan.FromDays(0), TimeSpan.FromDays(180));

//            //# use the underlying equity as the benchmark
//            base.SetBenchmark("GOOG");
//        }

//        public override void OnData(Slice slice)
//        {
//            if (base.Portfolio.Invested) return;

//            foreach (var kvp in slice.OptionChains)
//            {
//                if (kvp.Key != option_symbol) continue;
//                var chain = kvp.Value;

//                //# we sort the contracts to find at the money (ATM) contract with farthest expiration
//                //var contracts = chain,
//                //    x => abs(chain.Underlying.Price - x.Strike)),
//                //    x => x.Expiry, reverse: true),
//                //    x => x.Right, reverse: true);

//                var contracts = chain
//                    .OrderByDescending(x => x.Expiry)
//                    .ThenBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
//                    .ThenByDescending(x => x.Right)
//                    .ToArray();
//                //.FirstOrDefault();

//                //#if found, trade it
//                if (contracts.Count() == 0) continue;
//                var symbol = contracts[0].Symbol;
//                base.MarketOrder(symbol, 1);
//                base.MarketOnCloseOrder(symbol, -1);
//            }
//        }
//        public override void OnOrderEvent(OrderEvent orderEvent)
//        {
//            base.Log(orderEvent.ToString());
//        }
//    }

//    //Sell in May Algorithm Example:
//    public partial class QCUSellInMay : QCAlgorithm, IAlgorithm
//    {

//        //Algorithm Variables
//        //int quantity = 400;
//        int ownbond = 0;
//        int owndia = 0;
//        int season = 0;
//        private string symbol = "DIA";
//        //private string symbol = "IJR"; // ishares core S&P small cap etf
//        private string sbond = "AGG";  // agg is agreggate bond etf ishares us
//                                       //private string sbond = "PONDX";
//        private decimal cash = 100000;

//        public MovingAverageConvergenceDivergence _macd;

//        //Initialize the Strategy
//        public override void Initialize()
//        {
//            SetCash(cash);
//            SetStartDate(2000, 10, 10);
//            SetEndDate(2017, 10, 10);
//            AddSecurity(SecurityType.Equity, symbol, Resolution.Minute);
//            AddSecurity(SecurityType.Equity, sbond, Resolution.Minute);
//            _macd = MACD(symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily);
//        }

//        //Handle the data events:
//        public void OnData(TradeBars data)
//        {
//            if (Time.ToString("MMM") == "May") { season = 1; }
//            if (Time.ToString("MMM") == "Oct") { season = 2; }

//            if (data.ContainsKey(symbol) == false) return;

//            if (_macd < 0 && season == 1)
//            {
//                if (owndia == 1)
//                {
//                    Order(symbol, -Portfolio[symbol].Quantity); // sell DIA and then buy bond
//                    owndia = 0;
//                }

//                if (data.ContainsKey(sbond) == true)
//                {
//                    int quantity = (int)Math.Floor(Portfolio.Cash / data[sbond].Close);
//                    Order(sbond, quantity);
//                    ownbond = 1;
//                    Debug("QCU Sell In May: Flat " + quantity + Time.ToString("Y"));
//                }
//            }
//            else
//            {
//                if (_macd > 0 && season == 2)
//                {
//                    if (ownbond == 1) { Order(sbond, -Portfolio[sbond].Quantity); ownbond = 0; }// sell the bond fund and buy DIA
//                    int quantity = (int)Math.Floor(Portfolio.Cash / data[symbol].Close);
//                    Order(symbol, quantity);
//                    owndia = 1;
//                    Debug("QCU Sell In May: Long " + Time.ToString("Y"));
//                }
//            }
//        }
//    }
//}