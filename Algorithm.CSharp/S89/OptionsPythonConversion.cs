// 
// https://www.quantconnect.com/forum/discussion/3245/using-option-greeks-to-select-option-contracts-to-trade
// 
// if you need Greeks:
//  A) Filter and B) AddOption 
//     more efficient than 
//  C) OptionChainProvider and D) AddOptionContract
// 
namespace NQuantConnect.Algorithm.CSharpamespace
{
    using QuantConnect;
    using QuantConnect.Algorithm;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Orders;
    using QuantConnect.Securities;
    using QuantConnect.Securities.Option;
    using System;
    using System.Linq;
    // using d = @decimal;
    // using last_trading_day = my_calendar.last_trading_day;
    using OptionPriceModels = QuantConnect.Securities.Option.OptionPriceModels;
    // using timedelta = datetime.timedelta;

    public class DeltaHedgedStraddleAlgo
        : QCAlgorithm
    {
        private const string SPY = "SPY";
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

        public override void Initialize()
        {
            this.SetStartDate(2017, 1, 1);
            this.SetEndDate(2017, 3, 31);
            this.SetCash(1000000);
            this.Log("PERIOD: 2017");
            // ----------------------------------------------------------------------
            // Algo params
            // ----------------------------------------------------------------------
            this.PREMIUM = 0.01;
            this.MAX_EXPIRY = 30;
            this._no_K = 2;
            this.resol = Resolution.Minute;
            this.tkr = SPY;
            //this.Lev = d.Decimal(1.0);
            this.Lev = 1.0;
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
            // -----------------------------------------------------------------------------
            // scheduled functions
            // -----------------------------------------------------------------------------
            this.Schedule.On(this.DateRules.EveryDay(this.equity_symbol), this.TimeRules.BeforeMarketClose(this.equity_symbol, 10), new Action(this.close_options));
        }

        //  Liquidate opts (with some value) and underlying
        //         
        public void close_options()
        {
            // check this is the last trading day
            if (this.last_trading_day != this.Time.Date)
            {
                return;
            }
            this.Log("On last trading day: liquidate options with value and underlying ");
            // liquidate options (if invested and in the money [otherwise their price is min of $0.01)
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

        public override void OnData(Slice slice)
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
            if (!this.Portfolio.Invested)
            {
                // select contract
                this.Log("get contract");
                this.get_contracts(slice);
                //if (!this.call || !this.put)
                if (this.call  == null || this.put == null)
                {
                    return;
                }
                // trade
                var unit_price = this.Securities[this.equity_symbol].Price * 100.0m;
                var qnty = Convert.ToInt32(this.Portfolio.TotalPortfolioValue / unit_price);
                // call_exists, put_exists = self.call is not None, self.put is not None
                if (this.call != null)
                {
                    this.Sell(this.call.Symbol, qnty);
                }
                if (this.put != null)
                {
                    this.MarketOrder(this.put.Symbol, -qnty);
                }
            }
            // 3. delta-hedged any existing option
            if (this.Portfolio.Invested && this.HourMinuteIs(10, 1))
            {
                this.get_greeks(slice);
                if (Math.Abs(this.previous_delta - this.Delta) > this.delta_treshold)
                {
                    this.Log(String.Format("delta_hedging: self.call {0}, self.put {1}, self.Delta {2}", this.call.ToString(), this.put.ToString(), this.Delta.ToString()));
                    this.SetHoldings(this.equity_symbol, this.Delta);
                    this.previous_delta = this.Delta;
                }
            }
        }

        // 
        //         Get ATM call and put
        //         
        public virtual void get_contracts(Slice slice)
        {
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
                //this.last_trading_day = last_trading_day(this.expiry);
                this.last_trading_day = this.expiry;
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
                    this.Log("No traded cointracts");
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
            throw new NotImplementedException();
        }

        // ----------------------------------------------------------------------
        // Other ancillary fncts
        // ----------------------------------------------------------------------   
        public virtual void OnOrderEvent(OrderEvent orderEvent)
        {
            //   self.Log("Order Event -> {}" .format(orderEvent))
        }


        public virtual void OnAssignmentOrderEvent(OrderEvent assignmentEvent)
        {
            this.Log(assignmentEvent.ToString());
            this._assignedOption = true;
        }

        //   if self.isMarketOpen(self.equity_symbol):
        //       self.Liquidate(self.equity_symbol)
        public virtual bool TimeIs(int day, int  hour, int minute)
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
    }

}
