// 
// 
// refs
// # https://www.quantconnect.com/tutorials/strategy-library/volatility-risk-premium-effect
// # https://www.quantconnect.com/forum/discussion/2894/the-options-trading-strategy-based-on-macd-indicator/p1
// # https://www.quantconnect.com/tutorials/tutorial-series/applied-options
// # https://www.quantconnect.com/forum/discussion/5709/optionchain-is-empty/p1
// 

using timedelta = datetime.timedelta;

using np = numpy;

using pd = pandas;

using stats = scipy.stats;

using Accord.Statistics;
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm;
using QuantConnect;
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Orders;
using System;

public class OptionRouletteAlgorithm
        : QCAlgorithm {
        
        public List<object> atm_call;
        
        public List<object> atm_put;
        
        public List<object> otm_call;
        
        public List<object> otm_put;
        
        public None slice;
        
        public Symbol symbol;
        
        public override void Initialize() {
            this.SetStartDate(2017, 1, 15);
            this.SetEndDate(2017, 2, 15);
            //self.SetStartDate(2015, 1, 1)
            //self.SetEndDate(datetime.now().date() - timedelta(1))
            this.SetCash(100000);
            var equity = this.AddEquity("SPY", Resolution.Minute);
            var option = this.AddOption("SPY", Resolution.Minute);
            this.symbol = equity.Symbol;
            option.SetFilter(this.UniverseFunc);
            this.SetBenchmark(equity.Symbol);
            this.slice = null;
            // Define the Schedules
            this.Schedule.On(this.DateRules.WeekStart(this.symbol), this.TimeRules.AfterMarketOpen(this.symbol, 5), this.MyLiquidate);
            // Define the Schedules
            this.Schedule.On(this.DateRules.WeekStart(this.symbol), this.TimeRules.AfterMarketOpen(this.symbol, 10), this.MyTrade);
        }
        
        public override void OnData(Slice slice) {
            this.slice = slice;
            if (slice.OptionChains.Count > 0) {
            }
        }
        
        public override void OnAssignmentOrderEvent(OrderEvent assignmentEvent) {
            this.Log(assignmentEvent.ToString());
            this.MyLiquidate();
        }
        
        public override void OnOrderEvent(OrderEvent orderEvent) {
            this.Log(orderEvent.ToString());
        }
        
        public OptionFilterUniverse UniverseFunc(OptionFilterUniverse universe) {
            var price = this.Securities[this.symbol].Price;
            return universe.IncludeWeeklys().Strikes(-50, 50).Expiration(new timedelta(30), new timedelta(50));
            // TODO: read above api.
        }
        
        public void MyLiquidate() {
            foreach (var x in this.Portfolio) {
                if (x.Value.Invested) {
                    this.Liquidate(x.Key);
                }
            }
            // redundant?
            if (this.Portfolio[this.symbol].Invested) {
                this.Liquidate(this.symbol);
            }
            this.Log("MyLiquidate");
        }
        
        public void MyTrade() {
            object otm_put_strike;
            object otm_call_strike;
            object mylist;
            var slice = this.slice;
            if (slice == null) {
                return;
            }
            this.Log("MyTrade {} {}".Format(this.Portfolio.Invested, slice.OptionChains.Count));
            if (slice.OptionChains.Count == 0) {
                return;
            }
            foreach (var i in slice.OptionChains) {
                var chains = i.Value;
                if (!this.Portfolio.Invested) {
                    this.Log("trading!");
                    // divide option chains into call and put options 
                    var calls = chains.Where(x => x.Right == OptionRight.Call).ToList().ToList();
                    var puts = chains.Where(x => x.Right == OptionRight.Put).ToList().ToList();
                    // if lists are empty return
                    if (!calls || !puts) {
                        return;
                    }
                    var underlying_price = this.Securities[this.symbol].Price;
                    var expiries = (from i in puts
                        select i.Expiry).ToList();
                    // determine expiration date nearly one month
                    var expiry = Math.Min(expiries, key: x => Math.Abs((x.date() - this.Time.Date).days - 40));
                    var strikes = (from i in puts
                        select i.Strike).ToList();
                    // determine at-the-money strike
                    var strike = Math.Min(strikes, key: x => Math.Abs(x - underlying_price));
                    // compute probability
                    var hist = this.History(new List<Symbol> {
                        this.symbol
                    }, 252 * 5, Resolution.Daily);
                //var prct_changes = hist.loc[this.symbol]["close"].pct_change(40);
                Slice sliceHist = hist.FirstOrDefault();
                var prct_changes = hist.FirstOrDefault().
                // 68% = 1sd, 90% = 2sd.
                var _tup_1 = np.nanpercentile(prct_changes, new List<int> {
                        5,
                        32,
                        68,
                        95
                    });
                    var m2sd = _tup_1.Item1;
                    var m1sd = _tup_1.Item2;
                    var p1sd = _tup_1.Item3;
                    var p2sd = _tup_1.Item4;
                    // roulette logic
                    var optionStyle = np.random.choice(new List<string> {
                        "short_strangle",
                        "short_iron_condor",
                        "long_strangle",
                        "synthetic_long"
                    }, 1)[0];
                    var num = np.random.choice(new List<int> {
                        2,
                        5,
                        10
                    }, 1)[0];
                    // long volatility strategies ********************************
                    // why would you ever?
                    if (optionStyle == "synthetic_long") {
                        this.atm_put = (from i in puts
                            where i.Expiry == expiry && i.Strike == strike
                            select i).ToList();
                        this.atm_call = (from i in calls
                            where i.Expiry == expiry && i.Strike == strike
                            select i).ToList();
                        if (this.atm_put && this.atm_call) {
                            mylist = new List<List<object>> {
                                this.atm_put[0],
                                this.atm_call[0]
                            };
                            this.Log("{}".format((from x in mylist
                                select stats.percentileofscore(prct_changes, (x.Strike - underlying_price) / underlying_price)).ToList()));
                            this.Sell(this.atm_put[0].Symbol, num);
                            this.Buy(this.atm_call[0].Symbol, num);
                        }
                    }
                    if (optionStyle == "long_strangle") {
                        this.atm_put = (from i in puts
                            where i.Expiry == expiry && i.Strike == strike
                            select i).ToList();
                        this.atm_call = (from i in calls
                            where i.Expiry == expiry && i.Strike == strike
                            select i).ToList();
                        if (this.atm_put && this.atm_call) {
                            mylist = new List<List<object>> {
                                this.atm_put[0],
                                this.atm_call[0]
                            };
                            this.Log("{}".format((from x in mylist
                                select stats.percentileofscore(prct_changes, (x.Strike - underlying_price) / underlying_price)).ToList()));
                            this.Buy(this.atm_put[0].Symbol, num);
                            this.Buy(this.atm_call[0].Symbol, num);
                        }
                    }
                    // short volatility strategies ********************************
                    if (optionStyle == "short_iron_condor") {
                        otm_call_strike = min(strikes, key: x => Math.Abs(x - underlying_price + p2sd * underlying_price));
                        var atm_call_strike = min(strikes, key: x => Math.Abs(x - underlying_price + p1sd * underlying_price));
                        var atm_put_strike = min(strikes, key: x => Math.Abs(x - underlying_price + m1sd * underlying_price));
                        otm_put_strike = min(strikes, key: x => Math.Abs(x - underlying_price + m2sd * underlying_price));
                        this.otm_call = (from i in calls
                            where i.Expiry == expiry && i.Strike == otm_call_strike
                            select i).ToList();
                        this.atm_call = (from i in calls
                            where i.Expiry == expiry && i.Strike == atm_call_strike
                            select i).ToList();
                        this.atm_put = (from i in puts
                            where i.Expiry == expiry && i.Strike == atm_put_strike
                            select i).ToList();
                        this.otm_put = (from i in puts
                            where i.Expiry == expiry && i.Strike == otm_put_strike
                            select i).ToList();
                        if (this.atm_call && this.atm_put && this.otm_put && this.otm_call) {
                            mylist = new List<List<object>> {
                                this.otm_put[0],
                                this.atm_call[0],
                                this.atm_put[0],
                                this.otm_call[0]
                            };
                            this.Log("{}".format((from x in mylist
                                select stats.percentileofscore(prct_changes, (x.Strike - underlying_price) / underlying_price)).ToList()));
                            // TODO: log net profit and potential max loss.
                            // buy otm
                            this.Buy(this.otm_call[0].Symbol, num);
                            this.Buy(this.otm_put[0].Symbol, num);
                            // sell near atm
                            this.Sell(this.atm_call[0].Symbol, num);
                            this.Sell(this.atm_put[0].Symbol, num);
                        }
                    }
                    if (optionStyle == "short_strangle") {
                        otm_call_strike = min(strikes, key: x => Math.Abs(x - underlying_price + p2sd * underlying_price));
                        otm_put_strike = min(strikes, key: x => Math.Abs(x - underlying_price + m2sd * underlying_price));
                        this.otm_put = (from i in puts
                            where i.Expiry == expiry && i.Strike == otm_put_strike
                            select i).ToList();
                        this.otm_call = (from i in calls
                            where i.Expiry == expiry && i.Strike == otm_call_strike
                            select i).ToList();
                        if (this.otm_put && this.otm_call) {
                            mylist = new List<List<object>> {
                                this.otm_put[0],
                                this.otm_call[0]
                            };
                            this.Log("{}".format((from x in mylist
                                select stats.percentileofscore(prct_changes, (x.Strike - underlying_price) / underlying_price)).ToList()));
                            this.Sell(this.otm_put[0].Symbol, num);
                            this.Sell(this.otm_call[0].Symbol, num);
                        }
                    }
                }
            }
        }
}
