using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Option.StrategyMatcher;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp.S89
{
    /// <summary>
    /// Regression algorithm exercising an equity Call Calendar Spread option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionEquityCallCalendarSpreadRegressionAlgorithmS89 : OptionEquityBaseStrategyRegressionAlgorithmS89
    {
        private TimeSpan timedelta(int days)
        {
            return TimeSpan.FromDays(days);
        }
        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (equity != null)
            {
                this.RemoveSecurity(equity.Symbol);
                equity = null;
            }

            foreach (var kvp in ActiveSecurities)
            {
                if (kvp.Value.Type != SecurityType.Option)
                    this.RemoveSecurity(kvp.Value.Symbol);
            }

            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    var contracts = chain
                        .Where(contract => contract.Right == OptionRight.Call)
                        .OrderBy(x => x.Expiry)
                        .ThenBy(x => x.Strike)
                        .ToList();

                    var shortCall = contracts.FirstOrDefault();
                    var longCall = contracts.FirstOrDefault(contract => contract.Expiry > shortCall.Expiry && contract.Strike == shortCall.Strike);
                    if (shortCall == null || longCall == null) return;

                    var initialMargin = Portfolio.MarginRemaining;
                    var qty = (int)(0.0001m * initialMargin / ActiveSecurities[longCall.Symbol].Close);
                    MarketOrder(shortCall.Symbol, -qty);
                    MarketOrder(longCall.Symbol, +qty);
                }
            }


            var option_invested = (from x in this.Portfolio
                                   where x.Value.Invested && x.Value.Type == SecurityType.Option
                                   select x.Key).ToList();
            if (option_invested.Any())
            {
                if (this.Time + timedelta(4) > option_invested[0].ID.Date)
                {
                    //this.Liquidate(option_invested[0], "Too close to expiration");
                    this.Liquidate();
                }
            }

            var nonoption_invested = (from x in this.Portfolio
                                   where x.Value.Invested && x.Value.Type != SecurityType.Option
                                   select x.Key).ToList();

            foreach(var nonoption in nonoption_invested)
            {
                this.Liquidate(nonoption);

            }
        }


    }
    /// <summary>
    /// Base class for equity option strategy regression algorithms which holds some basic shared setup logic
    /// </summary>
    public abstract class OptionEquityBaseStrategyRegressionAlgorithmS89 : QCAlgorithm
    {
        protected Equity equity;
        protected decimal _paidFees;
        protected Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2019, 01, 24);
            SetEndDate(2021, 06, 24);
            SetCash(200000);
            SetBrokerageModel(Brokerages.BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            equity = AddEquity("AAPL");//, leverage: 4
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(u => u.Strikes(-3, +3)
                                   // Expiration method accepts TimeSpan objects or integer for days.
                                   // The following statements yield the same filtering criteria
                                   .Expiration(28, 180)
                                   .CallsOnly()
                                   //.IncludeWeeklys()
                                   );
        }

        protected decimal GetPriceSpreadDifference(params Symbol[] symbols)
        {
            var spreadPaid = 0m;
            foreach (var symbol in symbols)
            {
                var security = Securities[symbol];
                var actualQuantity = security.Holdings.AbsoluteQuantity;
                var spread = 0m;
                if (security.Holdings.IsLong)
                {
                    if (security.AskPrice != 0)
                    {
                        spread = security.Price - security.AskPrice;
                    }
                }
                else if (security.BidPrice != 0)
                {
                    spread = security.BidPrice - security.Price;
                }
                spreadPaid += spread * actualQuantity * security.SymbolProperties.ContractMultiplier;
            }

            return spreadPaid;
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _paidFees += orderEvent.OrderFee.Value.Amount;
                if (orderEvent.Symbol.SecurityType.IsOption())
                {
                    var security = Securities[orderEvent.Symbol];
                    var premiumPaid = orderEvent.Quantity * orderEvent.FillPrice * security.SymbolProperties.ContractMultiplier;
                    Log($"{orderEvent}. Premium paid: {premiumPaid}");
                    return;
                }
            }
            Log($"{orderEvent}");
        }
    }
}
