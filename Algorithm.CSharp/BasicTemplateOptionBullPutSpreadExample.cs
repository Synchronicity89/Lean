﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrate how to use Option Strategies (e.g. OptionStrategies.Straddle) helper classes to batch send orders for common strategies.
    /// It also shows how you can prefilter contracts easily based on strikes and expirations, and how you can inspect the
    /// option chain to pick a specific option contract to trade.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="option strategies" />
    /// <meta name="tag" content="filter selection" />
    public class BasicTemplateOptionBullPutSpreadExample : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        const string s = "GOOG";
        private Symbol _optionSymbol;
        string iSymbol = s;
        DateTime iTime;

        public override void Initialize()
        {
            AddSecurity(SecurityType.Option, s, Resolution.Minute);
            SetStartDate(2015, 12, 24);
            SetEndDate(2016, 12, 23);
            SetCash(1000000);

            var option = AddOption(s);
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(-2, +2, TimeSpan.Zero, TimeSpan.FromDays(180));

            // Adding this to reproduce GH issue #2314
            SetWarmup(TimeSpan.FromMinutes(1));

            // use the underlying equity as the benchmark
            SetBenchmark(s);
            AddEquity(iSymbol, Resolution.Minute);
        }

        public void OnData(TradeBars data)
        {
             if (IsMarketOpen(iSymbol) == false)
            {
                return;
            }

            if (IsNewBar(TimeSpan.FromHours(1)) == false)
            {
                return;
            }

            var price = Securities[iSymbol].Price;

            // If options were exercised and we were assigned to buy shares, sell them immediately

            if (Portfolio[iSymbol].Invested)
            {
                MarketOrder(iSymbol, -100);
            }

            if (Portfolio.Invested == false)
            {
                var contracts = OptionChainProvider.GetOptionContractList(iSymbol, Time);

                // Choose all contracts within a month and strike price $1 to $5 from current underlying price

                var atmPuts =
                    from c in contracts
                    where c.ID.OptionRight == OptionRight.Put
                    where price - c.ID.StrikePrice < 3 && price - c.ID.StrikePrice > 1
                    where (c.ID.Date - Time).TotalDays < 45 && (c.ID.Date - Time).TotalDays > 0
                    select c;

                // Choose all contracts within a month and strike price $1 to $5 from current underlying price

                var otmPuts =
                    from c in contracts
                    where c.ID.OptionRight == OptionRight.Put
                    where price - c.ID.StrikePrice < 7 && price - c.ID.StrikePrice > 5
                    where (c.ID.Date - Time).TotalDays < 45 && (c.ID.Date - Time).TotalDays > 0
                    select c;

                // Take ATM options with the MIN expiration date and MAX distance from underlying price

                var contractAtmPut = atmPuts
                    .OrderBy(o => o.ID.Date)
                    .ThenBy(o => price - o.ID.StrikePrice)
                    .FirstOrDefault();

                // Take OTM options with the MIN expiration date and MAX distance from underlying price

                var contractOtmPut = otmPuts
                    .OrderBy(o => o.ID.Date)
                    .ThenBy(o => price - o.ID.StrikePrice)
                    .FirstOrDefault();

                // If we found such options - open trade

                if (contractAtmPut != null &&
                    contractOtmPut != null)
                {
                    AddOptionContract(contractAtmPut, Resolution.Minute);
                    AddOptionContract(contractOtmPut, Resolution.Minute);
                    MarketOrder(contractAtmPut, -1);
                    MarketOrder(contractOtmPut, 1);
                }
            }
        }

        public bool IsNewBar(TimeSpan interval, int points = 1)
        {
            var date = Securities[_optionSymbol].LocalTime;

            if ((date - iTime).TotalSeconds > interval.TotalSeconds * points)
            {
                iTime = new DateTime(date.Ticks - date.Ticks % interval.Ticks, date.Kind);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "778"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-100%"},
            {"Drawdown", "6.900%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-6.860%"},
            {"Sharpe Ratio", "0"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$778.00"}
        };
    }
}
