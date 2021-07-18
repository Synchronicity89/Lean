// namespace QuantConnect.Algorithm.CSharp
// {
//     public class FormalOrangeHippopotamus : QCAlgorithm
//     {

//         public override void Initialize()
//         {
//             SetStartDate(2020, 1, 24);  //Set Start Date
//             SetStartDate(2020, 5, 24);  //Set Start Date
//             SetCash(100000);             //Set Strategy Cash

//             // AddEquity("SPY", Resolution.Minute);

// 			AddAlpha(new EmaCrossAlphaModel(50, 200, Resolution.Minute));

// 			AddAlpha(new MacdAlphaModel(12, 26, 9, MovingAverageType.Simple, Resolution.Daily));

// 			AddAlpha(new RsiAlphaModel(60, Resolution.Minute));

// 			SetExecution(new ImmediateExecutionModel());

// 			SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

// 			SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));

// 			SetUniverseSelection(new QC500UniverseSelectionModel());

//         }

//         /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
//         /// Slice object keyed by symbol containing the stock data
//         // public override void OnData(Slice data)
//         // {
//         //     // if (!Portfolio.Invested)
//         //     // {
//         //     //    SetHoldings("SPY", 1);
//         //     //    Debug("Purchased Stock");
//         //     //}
//         // }

//     }
// }

/*
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
*/

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template options framework algorithm uses framework components to define an algorithm
    /// that trades options.
    /// </summary>
    public class OptionsUniverseAlgo : QCAlgorithm
    {
        private Symbol _aapl;
        private Symbol _twx;
        public override void Initialize()
        {
            _twx = QuantConnect.Symbol.Create("TWX", SecurityType.Equity, Market.USA);
            _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            UniverseSettings.Resolution = Resolution.Minute;


            SetStartDate(2014, 06, 01);
            SetEndDate(2014, 06, 16);
            SetCash(100000);

            // set framework models
            //SetUniverseSelection(new EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(/*SelectOptionChainSymbols*/));
            var selectionUniverse = AddUniverse(enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl },
    enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl });
            AddUniverseOptions( selectionUniverse, universe =>
            {
                if (universe.Underlying == null)
                {
                    throw new Exception("Underlying data point is null! This shouldn't happen, each OptionChainUniverse handles and should provide this");
                }
                return universe.IncludeWeeklys()
                    .FrontMonth()
                    .Contracts(universe.Take(5));
            });
            // SetAlpha(new ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromHours(0.5)));
            AddAlpha(new MacdAlphaModel(12, 26, 9, MovingAverageType.Simple, Resolution.Hour));
            SetPortfolioConstruction(new SingleSharePortfolioConstructionModel());
            // SetExecution(new ImmediateExecutionModel());
            // SetRiskManagement(new NullRiskManagementModel());
            base.SetExecution(new StandardDeviationExecutionModel(60, 2.7m, Resolution.Minute));
            //base.AddRiskManagement(new MaximumDrawdownPercentPerSecurity(Decimal.Parse(GetParameter("DrawdownPerSecurity"))));
            base.AddRiskManagement(new TrailingStopRiskManagementModel());
        }

        public void AddUniverseOptions(Universe universe, Func<OptionFilterUniverse, OptionFilterUniverse> optionFilter)
        {
            AddUniverseSelection(new OptionChainedUniverseSelectionModel(universe, optionFilter));
        }

        // option symbol universe selection function
        //private static IEnumerable<Symbol> SelectOptionChainSymbols(DateTime utcTime)
        //{
        //    var newYorkTime = utcTime.ConvertFromUtc(TimeZones.NewYork);
        //    //if (newYorkTime.Date < new DateTime(2020, 03, 20))
        //    if (newYorkTime.Date >= new DateTime(2020, 01, 20))
        //    {
        //        yield return QuantConnect.Symbol.Create("TWX", SecurityType.Option, Market.USA, "?TWX");
        //    }

        //    if (newYorkTime.Date >= new DateTime(2020, 01, 20))
        //    {
        //        yield return QuantConnect.Symbol.Create("AAPL", SecurityType.Option, Market.USA, "?AAPL");
        //    }
        //}

        /// <summary>
        /// Creates option chain universes that select only the earliest expiry ATM weekly put contract
        /// and runs a user defined optionChainSymbolSelector every day to enable choosing different option chains
        /// </summary>
        class EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel : OptionUniverseSelectionModel
        {
            public EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector)
                : base(TimeSpan.FromDays(1), optionChainSymbolSelector)
            {
            }

            /// <summary>
            /// Defines the option chain universe filter
            /// </summary>
            protected override OptionFilterUniverse Filter(OptionFilterUniverse filter)
            {
                return filter
                    .Strikes(+1, +1)
                    // Expiration method accepts TimeSpan objects or integer for days.
                    // The following statements yield the same filtering criteria
                    .Expiration(0, 7)
                    //.Expiration(TimeSpan.Zero, TimeSpan.FromDays(7))
                    .WeeklysOnly()
                    .PutsOnly()
                    .OnlyApplyFilterAtMarketOpen();
            }
        }

        /// <summary>
        /// Implementation of a constant alpha model that only emits insights for option symbols
        /// </summary>
        class ConstantOptionContractAlphaModel : ConstantAlphaModel
        {
            public ConstantOptionContractAlphaModel(InsightType type, InsightDirection direction, TimeSpan period)
                : base(type, direction, period)
            {
            }

            protected override bool ShouldEmitInsight(DateTime utcTime, Symbol symbol)
            {
                // only emit alpha for option symbols and not underlying equity symbols
                if (symbol.SecurityType != SecurityType.Option)
                {
                    return false;
                }

                return base.ShouldEmitInsight(utcTime, symbol);
            }
        }

        /// <summary>
        /// Portfolio construction model that sets target quantities to 1 for up insights and -1 for down insights
        /// </summary>
        class SingleSharePortfolioConstructionModel : PortfolioConstructionModel
        {
            public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                foreach (var insight in insights)
                {
                    yield return new PortfolioTarget(insight.Symbol, (int)insight.Direction);
                }
            }
        }
    }
}
