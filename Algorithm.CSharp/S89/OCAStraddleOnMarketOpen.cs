// 
// https://www.quantconnect.com/forum/discussion/3245/using-option-greeks-to-select-option-contracts-to-trade
// 
// if you need Greeks:
//  A) Filter and B) AddOption 
//     more efficient than 
//  C) OptionChainProvider and D) AddOptionContract
// 
namespace QuantConnect.Algorithm.CSharp
{
    using QuantConnect;
    using QuantConnect.Algorithm;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Orders;
    using QuantConnect.Securities;
    using QuantConnect.Securities.Option;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    // using d = @decimal;
    // using last_trading_day = my_calendar.last_trading_day;
    using OptionPriceModels = QuantConnect.Securities.Option.OptionPriceModels;
    // using timedelta = datetime.timedelta;

    public class OCAStraddleOnMarketOpen
        : QCAlgorithm
    {
        private const int sever = 1; //Debug
        private const string SPY = "SPY";
        private const decimal TrailPercent = 1.0m + 0.06m;
        private double PREMIUM;
        private int MAX_EXPIRY;
        private int _no_K;
        private Resolution resol;
        private string tkr;
        private double Lev;
        private bool select_flag;
        private bool hedge_flag;
        private double previous_delta;
        private double delta_treshold;
        private Symbol equity_symbol;
        private Symbol option_symbol;
        private bool _assignedOption;
        private OptionContract call;
        private OptionContract put;
        private DateTime expiry;
        private DateTime last_trading_day;
        private Security equity;
        private double Delta;
        private List<OrderTicketInfo> OrderTickets = new List<OrderTicketInfo>();

        public override void Initialize()
        {
            this.SetStartDate(2017, 1, 1);
            this.SetEndDate(2017, 6, 28);
            this.SetCash(25000);
            this.LogCust("PERIOD: 2017");
            // ----------------------------------------------------------------------
            // Algo params
            // ----------------------------------------------------------------------
            this.PREMIUM = 0.01;
            this.MAX_EXPIRY = 30;
            this._no_K = 2;
            this.resol = Resolution.Minute;
            this.tkr = SPY;
            // self.Ntnl_perc = d.Decimal( round( 1. / (2. * self.MAX_EXPIRY/7.), 2) ) #  notional percentage, e.g. 0.08
            this.select_flag = false;
            this.hedge_flag = false;
            this.previous_delta = 0.0;
            this.delta_treshold = 0.05;
            // ----------------------------------------------------------------------
            // add underlying Equity 
            var equity = this.AddEquity(this.tkr, this.resol);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            this.equity_symbol = equity.Symbol;
            // Add options
            var option = this.AddOption(this.tkr, this.resol);
            this.option_symbol = option.Symbol;
            // set our strike/expiry filter for this option chain
            option.SetFilter(this.UniverseFunc);
            // for greeks and pricer (needs some warmup) - https://github.com/QuantConnect/Lean/blob/21cd972e99f70f007ce689bdaeeafe3cb4ea9c77/Common/Securities/Option/OptionPriceModels.cs#L81
            option.PriceModel = OptionPriceModels.CrankNicolsonFD();
            // this is needed for Greeks calcs
            this.SetWarmUp(TimeSpan.FromDays(3));
            this._assignedOption = false;
            this.call = null;
            this.put = null;
            OrderTickets.Add(new OrderTicketInfo { EquityUnderlying = equity });
            OrderTickets.Add(new OrderTicketInfo { EquityUnderlying = equity });
            // -----------------------------------------------------------------------------
            // scheduled functions
            // -----------------------------------------------------------------------------
            this.Schedule.On(this.DateRules.EveryDay(this.equity_symbol), this.TimeRules.AfterMarketOpen(this.equity_symbol, 60), new Action(this.close_optionsOpen));
            this.Schedule.On(this.DateRules.EveryDay(this.equity_symbol), this.TimeRules.BeforeMarketClose(this.equity_symbol, 10), new Action(this.close_optionsClose));
        }

        //  Liquidate opts (with some value) and underlying
        //         
        public void close_optionsOpen()
        {
            Liquid();
        }

        //  Liquidate opts (with some value) and underlying
        //         
        public void close_optionsClose()
        {
            // check this is the last trading day
            if (this.last_trading_day != this.Time.Date)
            {
                //return;
            }
            this.LogCust("On last trading day: liquidate options with value and underlying ");
            // liquidate options (if invested and in the money [otherwise their price is min of $0.01)
            Liquid();
            this.call = null;
            this.put = null;
            OrderTickets.Clear();
            OrderTickets.Add(new OrderTicketInfo { EquityUnderlying = equity });
            OrderTickets.Add(new OrderTicketInfo { EquityUnderlying = equity });
        }

        private void Liquid()
        {
            for (int i = 0; i < OrderTickets.Count; i++)
            {
                if (OrderTickets[i].OrderTicket != null && OrderTickets[i].OrderTicket.CancelRequest == null && OrderTickets[i].OrderTicket.AverageFillPrice == 0.0m)
                {
                    try
                    {
                        OrderTickets[i].OrderTicket.Cancel("Timed Out");
                    }
                    catch (ArgumentException ex)
                    {
                        if (this.LiveMode == true) this.Quit("Time out cancellation failed. " + ex.GetType().FullName + ": " + ex.Message);
                    }
                }
            }
            foreach (var x in this.Portfolio)
            {
                // symbol = x.Key; security = x.Value ## but also symbol = x.Value.Symbol 
                if (x.Value.Invested)
                {
                    //  self.Portfolio[opt].Invested, but no need for self.Securities.ContainsKey(opt)
                    // only liquidate valuable options, otherwise let them quietly expiry
                    if (this.Securities[x.Key].AskPrice > 0.05m)
                    {
                        this.Liquidate(x.Key);
                    }
                }
            }
            // CHECK if this necessary (incorporated above)
            if (this.Portfolio[this.equity_symbol].Invested)
            {
                this.Liquidate(this.equity.Symbol);
            }
        }

        int calledOnData = 0;
        int lastCalledOnData;

        bool TimeBetween(DateTime datetime, TimeSpan start, TimeSpan end)
        {
            // convert datetime to a TimeSpan
            TimeSpan now = datetime.TimeOfDay;
            // see if start comes before end
            if (start < end)
                return start <= now && now <= end;
            // start is after end, so do the inverse comparison
            return !(end < now && now < start);
        }
        public override void OnData(Slice slice)
        {
            if (calledOnData == 0) LogCust("called OnData");
            calledOnData++;
            if (this.IsWarmingUp) return;
            if (calledOnData == 1) LogCust("called OnData got past warmup");
            for (int i = 0; i < OrderTickets.Count; i++)
            {
                if (calledOnData == 1) LogCust("called OnData iterating OrderTickets");
                int contracts = 0;
                if (i % 2 == 0 && OrderTickets[i].OptionContract == null && OrderTickets[i].OrderTicket == null)
                {
                    OrderTickets[i].OptionContract = this.call;
                    this.call = null;
                    contracts++;
                    lastCalledOnData = calledOnData;
                    LogCust("contracts: " + contracts + " lastCalledOnData: " + lastCalledOnData);
                }
                if (i % 2 == 1 && OrderTickets[i].OptionContract == null && OrderTickets[i].OrderTicket == null)
                {
                    OrderTickets[i].OptionContract = this.put;
                    this.put = null;
                    contracts++;
                }
                if (OrderTickets.Where(ot => ot.OptionContract != null).Count() < 2)
                {
                    this.LogCust("no contracts");
                    if (!this.Portfolio.Invested)
                    {
                        // select contract
                        this.LogCust("get contract");
                        this.get_contracts(slice);
                        LogCust("No put/call", () => call == null || put == null);
                    }
                    //return;
                }

                //Quit("contracts: " + contracts.ToString());
                this.LogCust("early assignments");
                // 1. deal with any early assignments
                if (OrderTickets[i].AssignedOption)
                {
                    // close everything (perhaps instead should be close everything with the same underlying)
                    foreach (var x in this.Portfolio)
                    {
                        if (x.Value.Invested)
                        {
                            this.Liquidate(x.Key);
                        }
                    }
                    OrderTickets[i].AssignedOption = false;
                }
                if (OrderTickets[i].OptionContract == null) continue;
                TimeSpan start = new TimeSpan(9, 30, 0);
                TimeSpan end = new TimeSpan(9, 40, 0);
                bool isBeginningOfDay = TimeBetween(Time, start, end);
                if (isBeginningOfDay == false) continue;

                var unit_price = this.Securities[OrderTickets[i].OptionContract.Symbol].Price * 100.0m;
                var qnty = Convert.ToInt32(this.Portfolio.TotalPortfolioValue / (20 * unit_price));

                if (/*OrderTickets[i].TimeFilled == DateTime.MinValue && */OrderTickets[i].OrderTicket == null)
                {
                    var a = OrderTickets[i].OptionContract;
                    OrderTickets[i].HighestPrice = MidPrice(OrderTickets[i].OptionContract);
                    OrderTickets[i].OrderTicket = StopMarketOrder(OrderTickets[i].OptionContract.Symbol, qnty, OrderTickets[i].HighestPrice * TrailPercent);
                }
                else
                {
                    if (MidPrice(OrderTickets[i].OptionContract) > OrderTickets[i].HighestPrice)
                    {
                        var prev = OrderTickets[i].HighestPrice;
                        OrderTickets[i].HighestPrice = MidPrice(OrderTickets[i].OptionContract);
                        LogCust("higher price: " + OrderTickets[i].HighestPrice + "vs." + prev);
                        OrderTickets[i].OrderTicket.Update(new UpdateOrderFields()
                        {
                            StopPrice = TrailPercent * OrderTickets[i].HighestPrice
                        });
                    }
                }
            }
        }

/// <summary>
/// Log messages for debugging purpose
/// </summary>
/// <param name="message"></param>
/// <param name="assert"></param>
/// <param name="severity">0 info, 1 debug, 2 error</param>
        private void LogCust(string message, Func<bool> assert = null, int severity = 0)
        {
            bool fail = true;
            if (assert != null && !assert())
            {
                fail = false;
            }
            if (fail && severity >= sever) {
                if (severity == 2) Error(message); else Debug(message);
            };
        }


        private decimal MidPrice(OptionContract optionContract)
        {
            return (optionContract.AskPrice + optionContract.BidPrice) / 2;
        }



        // 
        //         Get ATM call and put
        //         
        public virtual void get_contracts(Slice slice)
        {
            LogCust("called get contracts");
            foreach (var kvp in slice.OptionChains)
            {
                if (kvp.Key != this.option_symbol)
                {
                    continue;
                }
                var chain = kvp.Value;
                var spot_price = chain.Underlying.Price;
                //   self.Log("spot_price {}" .format(spot_price))
                // prefer to do in steps, rather than a nested sorted
                // 1. get furthest expiry            
                var contracts_by_T = chain.OrderByDescending(x => x.Expiry).ToList();
                if (!(contracts_by_T.Count > 0))
                {
                    return;
                }
                this.expiry = contracts_by_T[0].Expiry.Date;

                var weeklySymbol = QuantConnect.Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 200, this.expiry);
                //var monthlysymbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 200, new DateTime(2020, 04, 17));

                //Assert.True(OptionSymbol.IsWeekly(weeklySymbol));
                //Assert.False(OptionSymbol.IsWeekly(monthlysymbol));

                //Assert.AreEqual(new DateTime(2020, 04, 17)/*Friday*/, OptionSymbol.GetLastDayOfTrading(monthlysymbol));
                //Good Friday on 10th so should be 9th
                //Assert.AreEqual(new DateTime(2020, 04, 09)/*Thursday*/, OptionSymbol.GetLastDayOfTrading(weeklySymbol));


                //this.last_trading_day = last_trading_day(this.expiry);
                this.last_trading_day = OptionSymbol.GetLastDayOfTrading(weeklySymbol);
                // get contracts with further expiry and sort them by strike
                var slice_T = (from i in chain
                               where i.Expiry.Date == this.expiry
                               select i).ToList();
                var sorted_contracts = slice_T.OrderBy(x => x.Strike).ToList();
                //   self.Log("Expiry used: {} and shortest {}" .format(self.expiry, contracts_by_T[-1].Expiry.date()) )
                // 2a. get the ATM closest CALL to short
                var calls = (from i in sorted_contracts
                             where i.Right == OptionRight.Call && i.Strike >= spot_price
                             select i).ToList();
                //this.call = calls ? calls[0] : null;
                this.call = calls.Count > 0 ? calls.First() : null;
                //   self.Log("delta call {}, self.call type {}" .format(self.call.Greeks.Delta, type(self.call)))
                //   self.Log("implied vol {} " .format(self.call.ImpliedVolatility))
                // 2b. get the ATM closest put to short
                var puts = (from i in sorted_contracts
                            where i.Right == OptionRight.Put && i.Strike <= spot_price
                            select i).ToList();
                this.put = puts.Count > 0 ? puts.Last() : null;
                //if (put == null) this.Quit("put == null");
            }
        }

        // 
        //         Get greeks for invested option: self.call and self.put
        //         
        public virtual void get_greeks(Slice slice)
        {
            if (this.call == null || this.put == null)
            {
                return;
            }
            foreach (var kvp in slice.OptionChains)
            {
                if (kvp.Key != this.option_symbol)
                {
                    continue;
                }
                var chain = kvp.Value;
                var traded_contracts = chain.Where(x => x.Symbol == this.call.Symbol || x.Symbol == this.put.Symbol).ToList();
                if (!(traded_contracts.Count > 0))
                {
                    this.LogCust("No traded contracts");
                    return;
                }
                var deltas = (from i in traded_contracts
                              select i.Greeks.Delta).ToList();
                //   self.Log("Delta: {}" .format(deltas))
                this.Delta = (double)deltas.Sum();
                // self.Log("Vega: " + str([i.Greeks.Vega for i in contracts]))
                // self.Log("Gamma: " + str([i.Greeks.Gamma for i in contracts]))
            }
        }

        public OptionFilterUniverse UniverseFunc(OptionFilterUniverse universe)
        {
            return universe.IncludeWeeklys().Strikes(-this._no_K, this._no_K).Expiration(timedelta(1), timedelta(this.MAX_EXPIRY));
        }

        private TimeSpan timedelta(int v)
        {
            return TimeSpan.FromDays(v);
        }

        // ----------------------------------------------------------------------
        // Other ancillary fncts
        // ----------------------------------------------------------------------   
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
                return;
            foreach (OrderTicketInfo stopMarketTicket in this.OrderTickets)
            {
                if (stopMarketTicket.OrderTicket == null) continue;
                if (stopMarketTicket != null && orderEvent.OrderId == stopMarketTicket.OrderTicket.OrderId)
                {
                    stopMarketTicket.TimeFilled = Time;
                }
            }

            foreach (OrderTicketInfo stopMarketTicket in this.OrderTickets)
            {
                if (stopMarketTicket.OrderTicket == null) continue;
                if (stopMarketTicket != null && orderEvent.OrderId != stopMarketTicket.OrderTicket.OrderId && stopMarketTicket.OrderTicket.QuantityFilled == 0.00m)
                {
                    try
                    {
                        if (stopMarketTicket.Canceled == true) continue;
                        stopMarketTicket.OrderTicket.Cancel("OCA");
                        stopMarketTicket.OrderTicket = null;
                        stopMarketTicket.Canceled = true;
                    }
                    catch (ArgumentException ex)
                    {
                        //backtesting doesn't simulate it perfectly.  Exception shouldn't happen when live.  Let's hope not...
                        if (this.LiveMode == true) Quit(ex.GetType().ToString() + ": " + ex.Message);
                    }
                }
            }

        }


        public override void OnAssignmentOrderEvent(OrderEvent assignmentEvent)
        {
            this.LogCust(assignmentEvent.ToString());
            this._assignedOption = true;
        }

        //   if self.isMarketOpen(self.equity_symbol):
        //       self.Liquidate(self.equity_symbol)
        public virtual bool TimeIs(int day, int hour, int minute)
        {
            return this.Time.Day == day && this.Time.Hour == hour && this.Time.Minute == minute;
        }

        public virtual bool HourMinuteIs(int hour, int minute)
        {
            return this.Time.Hour == hour && this.Time.Minute == minute;
            // ----------------------------------------------------------------------
            // all_symbols = [ x.Value for x in self.Portfolio.Keys ]
            // all_invested = [x.Symbol.Value for x in self.Portfolio.Values if x.Invested ]
            // for kvp in self.Securities: symbol = kvp.Key; security = kvp.Value
            //
            // orders = self.Transactions.GetOrders(None)
            // for order in orders: self.Log("order symbol {}" .format(order.Symbol))
            //
            // volatility = self.Securities[self.equity_symbol].VolatilityModel.Volatility
            // self.Log("Volatility: {}" .format(volatility))
        }

        public void OnDataTrailOLD(Slice slice, List<OptionContract> optionContracts, int qnty)
        {

            //foreach (OrderTicketInfo stopMarketTicket in this.OrderTickets)
            for (int i = 0; i < optionContracts.Count; i++)
            {
                OrderTicketInfo stopMarketTicket = new OrderTicketInfo();

                //1. Plot the current SPY price to "Data Chart" on series "Asset Price"
                Plot("Levels", "Asset Price", Securities["SPY"].Price);
                Plot("Levels", "Stop Price", Securities["SPY"].Price * 0.9m);
                if ((Time - stopMarketTicket.TimeFilled).TotalMinutes < 65)
                {
                    //find a way to reduce flailing
                    //return;
                }
                //if (!Portfolio.Invested)
                if (stopMarketTicket.TimeFilled == DateTime.MinValue && stopMarketTicket.OrderTicket == null)
                {

                    MarketOrder("SPY", 500);
                    stopMarketTicket.HighestPrice = Securities["SPY"].Close;
                    stopMarketTicket.OrderTicket = StopMarketOrder("SPY", -500, stopMarketTicket.HighestPrice * 0.9m);

                }
                else
                {

                    //2. Plot the moving stop price on "Data Chart" with "Stop Price" series name

                    if (Securities["SPY"].Close > stopMarketTicket.HighestPrice)
                    {
                        stopMarketTicket.HighestPrice = Securities["SPY"].Close;

                        stopMarketTicket.OrderTicket.Update(new UpdateOrderFields()
                        {
                            StopPrice = 0.9m * stopMarketTicket.HighestPrice
                        });
                    }
                }
            }
        }

        public void OnDataOLD(Slice slice)
        {
            if (this.IsWarmingUp)
            {
                return;
            }
            // 1. deal with any early assignments
            if (this._assignedOption)
            {
                // close everything
                foreach (var x in this.Portfolio)
                {
                    if (x.Value.Invested)
                    {
                        this.Liquidate(x.Key);
                    }
                }
                this._assignedOption = false;
            }
            //   self.call, self.put = None, None  # stop getting Greeks
            // 2. sell options, if none

            var unit_price = this.Securities[this.equity_symbol].Price * 100.0m;
            var qnty = Convert.ToInt32(this.Portfolio.TotalPortfolioValue / unit_price);
            if (!this.Portfolio.Invested)
            {
                // select contract
                this.LogCust("get contract");
                this.get_contracts(slice);
                //if (!this.call || !this.put)
                if (this.put == null || this.call == null)
                {
                    return;
                }
                // trade

                // call_exists, put_exists = self.call is not None, self.put is not None
                // if call had been null it would have returned above, so if not really needed
                //if (this.call != null)
                //{
                //
                //this.Sell(this.call.Symbol, qnty);
                //this.StopMarketOrder(this.call.Symbol, qnty);
                ////}
                //// if put had been null it would have returned above, so if not really needed surprised that VS 2019 doesn't warn
                //if (this.put != null)
                //{
                //    this.MarketOrder(this.put.Symbol, -qnty);
                //}
            }
            OnDataTrailOLD(slice, new List<OptionContract>(new OptionContract[] { this.call, this.put }), qnty);
            //public override void OnOrderEvent(OrderEvent orderEvent)
            //{
            //    if (orderEvent.Status != OrderStatus.Filled)
            //        return;

            //    if (stopMarketTicket != null && orderEvent.OrderId == stopMarketTicket.OrderId)
            //    {
            //        stopMarketOrderFilled = Time;
            //    }
            //}

            // 3. delta-hedged any existing option
            if (this.Portfolio.Invested && this.HourMinuteIs(10, 1))
            {
                this.get_greeks(slice);
                if (Math.Abs(this.previous_delta - this.Delta) > this.delta_treshold)
                {
                    this.LogCust(String.Format("delta_hedging: self.call {0}, self.put {1}, self.Delta {2}", this.call.ToString(), this.put.ToString(), this.Delta.ToString()));
                    this.SetHoldings(this.equity_symbol, this.Delta);
                    this.previous_delta = this.Delta;
                }
            }
        }
    }
    public class OrderTicketInfo
    {
        public OrderTicket OrderTicket { get; set; }
        public DateTime TimeFilled { get; set; } = DateTime.MinValue;
        public decimal HighestPrice { get; set; }
        public decimal LowestPrice { get; set; }
        public OptionContract OptionContract { get; set; }
        public bool AssignedOption { get; set; }
        public Security EquityUnderlying { get; set; }
        public bool Canceled { get; set; }
    }

}
