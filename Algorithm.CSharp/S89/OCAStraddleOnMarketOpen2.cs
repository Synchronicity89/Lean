// 
// https://www.quantconnect.com/forum/discussion/3245/using-option-greeks-to-select-option-contracts-to-trade
// 
// if you need Greeks:
//  A) Filter and B) AddOption 
//     more efficient than 
//  C) OptionChainProvider and D) AddOptionContract
// 
namespace QuantConnect.Algorithm.CSharp.S89
{
    using QuantConnect;
    using QuantConnect.Algorithm;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Orders;
    using QuantConnect.Securities;
    using QuantConnect.Securities.Equity;
    using QuantConnect.Securities.Option;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OptionPriceModels = QuantConnect.Securities.Option.OptionPriceModels;

    public class OCAStraddleOnMarketOpen2
        : QCAlgorithmS89
    {
        private const string SPY = "SPY";
        private const decimal TrailPercent = 1.0m + 0.06m;
        private int MAX_EXPIRY;
        private int _no_K;
        private Resolution resol;
        private string tkr;
        private Symbol equity_symbol;
        private Symbol option_symbol;
        private OptionContract call;
        private OptionContract put;
        private DateTime expiry;
        private Equity equity;
        private List<OrderTicketInfo2> OrderTickets = new List<OrderTicketInfo2>();

        public override void Initialize()
        {
            this.SetStartDate(2017, 1, 1);
            this.SetEndDate(2017, 6, 28);
            this.SetCash(25000);
            this.LogCust("PERIOD: 2017");
            // ----------------------------------------------------------------------
            // Algo params
            // ----------------------------------------------------------------------
            this.MAX_EXPIRY = 30;
            this._no_K = 2;
            this.resol = Resolution.Minute;
            this.tkr = SPY;
            // ----------------------------------------------------------------------
            // add underlying Equity 
            equity = this.AddEquity(this.tkr, this.resol);
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
            this.call = null;
            this.put = null;
            Combo straddle;
            OrderTickets.Add(new OrderTicketInfo2 { EquityUnderlying = equity });
            OrderTickets.Add(new OrderTicketInfo2 { EquityUnderlying = equity });
            // -----------------------------------------------------------------------------
            // scheduled functions
            // -----------------------------------------------------------------------------
            this.Schedule.On(this.DateRules.EveryDay(this.equity_symbol), this.TimeRules.AfterMarketOpen(this.equity_symbol, 60), new Action(this.CloseOptionsOpen));
            this.Schedule.On(this.DateRules.EveryDay(this.equity_symbol), this.TimeRules.BeforeMarketClose(this.equity_symbol, 10), new Action(this.closeOptionsClose));
        }

        //  Liquidate opts (with some value) and underlying
        //         
        public void CloseOptionsOpen()
        {
            Liquid();
        }
        
        public void closeOptionsClose()
        {
            this.LogCust("On last trading day: liquidate options with value and underlying ");
            // liquidate options (if invested and in the money [otherwise their price is min of $0.01)
            Liquid();
            this.call = null;
            this.put = null;
            OrderTickets.Clear();
            OrderTickets.Add(new OrderTicketInfo2 { EquityUnderlying = equity });
            OrderTickets.Add(new OrderTicketInfo2 { EquityUnderlying = equity });
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

        public override void OnData(Slice slice)
        {
            if (this.IsWarmingUp) return;
            for (int i = 0; i < OrderTickets.Count; i++)
            {
                if (i % 2 == 0 && OrderTickets[i].OptionContract == null && OrderTickets[i].OrderTicket == null)
                {
                    OrderTickets[i].OptionContract = this.call;
                    this.call = null;
                }
                if (i % 2 == 1 && OrderTickets[i].OptionContract == null && OrderTickets[i].OrderTicket == null)
                {
                    OrderTickets[i].OptionContract = this.put;
                    this.put = null;
                }
                if (OrderTickets.Where(ot => ot.OptionContract != null).Count() < 2)
                {
                    this.LogCust("no contracts");
                    if (!this.Portfolio.Invested)
                    {
                        // select contract
                        this.LogCust("get contract");
                        this.GetContracts(slice);
                        LogCust("No put/call", () => call == null || put == null);
                    }
                }

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
                bool isBeginningOfDay = Util.TimeBetween(Time, start, end);
                if (isBeginningOfDay == false) continue;

                var unit_price = this.Securities[OrderTickets[i].OptionContract.Symbol].Price * 100.0m;
                var qnty = Convert.ToInt32(this.Portfolio.TotalPortfolioValue / (20 * unit_price));

                if (OrderTickets[i].OrderTicket == null)
                {
                    var a = OrderTickets[i].OptionContract;
                    OrderTickets[i].HighestPrice = Util.MidPrice(OrderTickets[i].OptionContract);
                    OrderTickets[i].OrderTicket = StopMarketOrder(OrderTickets[i].OptionContract.Symbol, qnty, OrderTickets[i].HighestPrice * TrailPercent);
                }
                else
                {
                    if (Util.MidPrice(OrderTickets[i].OptionContract) > OrderTickets[i].HighestPrice)
                    {
                        var prev = OrderTickets[i].HighestPrice;
                        OrderTickets[i].HighestPrice = Util.MidPrice(OrderTickets[i].OptionContract);
                        LogCust("higher price: " + OrderTickets[i].HighestPrice + "vs." + prev);
                        OrderTickets[i].OrderTicket.Update(new UpdateOrderFields()
                        {
                            StopPrice = TrailPercent * OrderTickets[i].HighestPrice
                        });
                    }
                }
            }
        }

        // 
        //         Get ATM call and put
        //         
        public virtual void GetContracts(Slice slice)
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

                // prefer to do in steps, rather than a nested sorted
                // 1. get furthest expiry            
                var contracts_by_T = chain.OrderByDescending(x => x.Expiry).ToList();
                if (!(contracts_by_T.Count > 0))
                {
                    return;
                }
                this.expiry = contracts_by_T[0].Expiry.Date;

                // get contracts with further expiry and sort them by strike
                var slice_T = (from i in chain
                               where i.Expiry.Date == this.expiry
                               select i).ToList();
                var sorted_contracts = slice_T.OrderBy(x => x.Strike).ToList();

                // 2a. get the ATM closest CALL to short
                var calls = (from i in sorted_contracts
                             where i.Right == OptionRight.Call && i.Strike >= spot_price
                             select i).ToList();

                this.call = calls.Count > 0 ? calls.First() : null;

                // 2b. get the ATM closest put to short
                var puts = (from i in sorted_contracts
                            where i.Right == OptionRight.Put && i.Strike <= spot_price
                            select i).ToList();
                this.put = puts.Count > 0 ? puts.Last() : null;
            }
        }

        public OptionFilterUniverse UniverseFunc(OptionFilterUniverse universe)
        {
            return universe.IncludeWeeklys().Strikes(-this._no_K, this._no_K).Expiration(Util.timedelta(1), Util.timedelta(this.MAX_EXPIRY));
        }

        // ----------------------------------------------------------------------
        // Other ancillary fncts
        // ----------------------------------------------------------------------   
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
                return;
            foreach (OrderTicketInfo2 stopMarketTicket in this.OrderTickets)
            {
                if (stopMarketTicket.OrderTicket == null) continue;
                if (stopMarketTicket != null && orderEvent.OrderId == stopMarketTicket.OrderTicket.OrderId)
                {
                    stopMarketTicket.TimeFilled = Time;
                }
            }

            foreach (OrderTicketInfo2 stopMarketTicket in this.OrderTickets)
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

            foreach (OrderTicketInfo2 stopMarketTicket in this.OrderTickets)
            {
                if (stopMarketTicket.OrderTicket == null) continue;
                if (stopMarketTicket != null && assignmentEvent.OrderId == stopMarketTicket.OrderTicket.OrderId)
                {
                    stopMarketTicket.AssignedOption = true;
                }
            }
        }
    }
}
