

namespace QuantConnect.Algorithm.CSharp
{
    using QuantConnect.Data;
    using System;

    using QuantConnect.Algorithm;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using QuantConnect.Securities;

    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash. This is a skeleton
    /// framework you can use for designing an algorithm.
    /// </summary>
    public class GettingStarted : QCAlgorithm
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private Symbol OptionsSymbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2014, 10, 11);    //Set End Date
            SetCash(1000);             //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            AddEquity("SPY", Resolution.Minute);

            // There are other assets with similar methods. See "Selecting Options" etc for more details.
            // AddFuture, AddForex, AddCfd, AddOption
            // Complete Add Option API - Including Default Parameters:
			var option = AddOption("GOOG");
			option.SetFilter(-2, 2, TimeSpan.Zero, TimeSpan.FromDays(182));
			// or Linq
            //OptionFilterUniverse
			option.SetFilter(universe => from symbol in universe
			                                .WeeklysOnly()
			                                .Expiration(TimeSpan.Zero, TimeSpan.FromDays(10))
			                                    where symbol.ID.OptionRight != OptionRight.Put &&
			                                    universe.Underlying.Price - symbol.ID.StrikePrice < 60
			                                    select symbol);
			OptionsSymbol = option.Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
                Debug("Purchased Stock");
            }
            
            OptionChain chain;
			if (data.OptionChains.TryGetValue(OptionsSymbol, out chain))
			{
			    // we find at the money (ATM) put contract with farthest expiration
			    var atmContract = chain
			        .OrderByDescending(x => x.Expiry)
			        .ThenBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
			        .ThenByDescending(x => x.Right)
			        .FirstOrDefault();
			}
        }
    }
}