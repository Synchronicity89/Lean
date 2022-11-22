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
using System.Linq;
using NodaTime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template options framework algorithm uses framework components to define an algorithm
    /// that trades options.
    /// </summary>
    public class OCAStraddleFramework : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2019, 03, 05);
            SetEndDate(2019, 09, 06);
            SetCash(100000);

            // set framework models
            AddUniverseSelection(new EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(SelectOptionChainSymbols));
            SetAlpha(new EMAContractAlphaModel(2, 8, Resolution.Minute));
            SetPortfolioConstruction(new SingleSharePortfolioConstructionModel(10));
            SetExecution(new StandardDeviationExecutionModelS89(60, .05m, Resolution.Minute));
            //SetRiskManagement(new NullRiskManagementModel());
            //Rely on Call or Put being out of the money to prevent loss.   50% stop loss might help prevent theta decay
            SetRiskManagement(new TrailingStopProfitTakerRiskManagementModel(0.10m, 0.20m));

        }

        // option symbol universe selection function
        private static IEnumerable<Symbol> SelectOptionChainSymbols(DateTime utcTime)
        {
            var newYorkTime = utcTime.ConvertFromUtc(TimeZones.NewYork);
            if (newYorkTime.Date < new DateTime(2019, 06, 16))
            {
                yield return QuantConnect.Symbol.Create("SPY", SecurityType.Option, Market.USA, "?SPY");
            }

            if (newYorkTime.Date >= new DateTime(2019, 06, 16))
            {
                yield return QuantConnect.Symbol.Create("AAPL", SecurityType.Option, Market.USA, "?AAPL");
            }
        }

        /// <summary>
        /// Creates option chain universes that select only the earliest expiry ATM weekly put contract
        /// and runs a user defined optionChainSymbolSelector every day to enable choosing different option chains
        /// </summary>
        class EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel : OptionUniverseSelectionModel
        {
            public EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector)
                : base(TimeSpan.FromDays(30), optionChainSymbolSelector)
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
                    //.PutsOnly()
                    .OnlyApplyFilterAtMarketOpen();
            }
        }

        /// <summary>
        /// Implementation of a constant alpha model that only emits insights for option symbols
        /// </summary>
        class EMAContractAlphaModel : EmaCrossAlphaModel
        {
            public EMAContractAlphaModel(int fastMA, int slowMA, Resolution resolution)
                : base(fastMA, slowMA, resolution)
            {
            }
            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                var insights = base.Update(algorithm, data);
                List<Insight> toReturn = new List<Insight>();
                foreach (var insight in insights)
                {
                    if (ShouldEmitInsight(algorithm, insight, algorithm.Time.ConvertToUtc(TimeZones.NewYork), insight.Symbol))
                    {
                        toReturn.Add(insight);

                    }
                }
                return toReturn;
            }
            protected static bool ShouldEmitInsight(QCAlgorithm algorithm, Insight insight, DateTime utcTime, Symbol symbol)
            {
                // only emit alpha for option symbols and not underlying equity symbols
                if (symbol.SecurityType != SecurityType.Option || utcTime.ConvertFromUtc(TimeZones.NewYork).TimeOfDay > TimeSpan.FromHours(9.52) ||
                    utcTime.ConvertFromUtc(TimeZones.NewYork).TimeOfDay < TimeSpan.FromHours(9.5))
                {
                    return false;
                }

                //if (symbol.ToString().Contains('C') && insight.Direction == InsightDirection.Down && !algorithm.Portfolio[insight.Symbol].IsLong) return false;
                //if (symbol.ToString().Contains('P') && insight.Direction == InsightDirection.Up && !algorithm.Portfolio[insight.Symbol].IsShort) return false;
                if (insight.Direction == InsightDirection.Flat) return false;

                return true;
                //return base.ShouldEmitInsight(utcTime, symbol)
            }
        }

        /// <summary>
        /// Portfolio construction model that sets target quantities to 1 for up insights and -1 for down insights
        /// </summary>
        class SingleSharePortfolioConstructionModel : PortfolioConstructionModel
        {
            private int multiplier;

            public SingleSharePortfolioConstructionModel(int mult)
            {
                this.multiplier = mult;
            }

            public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                var timeOfDay = algorithm.Time.TimeOfDay;
                int mult = multiplier;
                // if (timeOfDay < TimeSpan.FromHours(9.52)) mult = multiplier;
                foreach (var insight in insights)
                {
                    //if (mult != multiplier) continue;
                    yield return new PortfolioTarget(insight.Symbol, mult * (int)insight.Direction);
                }
            }
            protected override bool ShouldCreateTargetForInsight(Insight insight)
            {
                return base.ShouldCreateTargetForInsight(insight);
            }
        }

        class StandardDeviationExecutionModelS89 : StandardDeviationExecutionModel
        {
            public StandardDeviationExecutionModelS89(int period, decimal deviation, Resolution resolution) : base(period, deviation, resolution)
            {

            }
            public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
            {
                List<IPortfolioTarget> newTargets = new List<IPortfolioTarget>();
                foreach (var target in targets)
                {
                    bool callNotPut = target.Symbol.Contains("C");
                    bool buying = target.Quantity > 0;
                    bool holdingsLong = algorithm.Portfolio.ContainsKey(target.Symbol) && algorithm.Portfolio[target.Symbol].IsLong;
                    bool holdingsshort = algorithm.Portfolio.ContainsKey(target.Symbol) && algorithm.Portfolio[target.Symbol].IsShort;

                    if (buying ^ holdingsLong)
                    {
                        newTargets.Add(target);
                    }
                }
                bool buyOnly = algorithm.Time.TimeOfDay < TimeSpan.FromHours(9.52) && algorithm.Time.TimeOfDay >= TimeSpan.FromHours(9.30);
                bool sellOnly = algorithm.Time.TimeOfDay >= TimeSpan.FromHours(9.52) && algorithm.Time.TimeOfDay < TimeSpan.FromHours(10.30);
                bool sellAll = algorithm.Time.TimeOfDay >= TimeSpan.FromHours(10.30);
                if (!sellAll)
                {
                    if (buyOnly)
                    {
                        base.Execute(algorithm, newTargets.Where(t => t.Quantity > 0).ToArray());
                    }
                    if (sellOnly)
                    {
                        base.Execute(algorithm, newTargets.Where(t => t.Quantity <= 0).ToArray());
                    }
                }
                else
                {
                    algorithm.Liquidate();
                }
            }

            protected override bool IsSafeToRemove(QCAlgorithm algorithm, Symbol symbol)
            {
                return base.IsSafeToRemove(algorithm, symbol);
            }

            protected override bool PriceIsFavorable(SymbolData data, decimal unorderedQuantity)
            {
                return base.PriceIsFavorable(data, unorderedQuantity);
            }
        }

        class TrailingStopProfitTakerRiskManagementModel : RiskManagementModel
        {
            public TrailingStopProfitTakerRiskManagementModel(decimal drawDown, decimal profitTaker)
            {
                _maximumDrawdownPercent = drawDown;
                _minimumProfitTakerPercent = profitTaker;
            }

            private readonly decimal _maximumDrawdownPercent;
            private readonly decimal _minimumProfitTakerPercent;
            private Dictionary<Symbol, decimal> _trailingHighs = new Dictionary<Symbol, decimal>();

            public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
            {
                foreach (var kvp in algorithm.Securities)
                {
                    var symbol = kvp.Key;
                    var security = kvp.Value;

                    // Remove if not invested
                    if (!security.Invested)
                    {
                        if (_trailingHighs.ContainsKey(symbol))
                        {
                            _trailingHighs.Remove(symbol);
                        }
                        continue;
                    }

                    // Add newly invested securities
                    if (!_trailingHighs.ContainsKey(symbol))
                    {
                        _trailingHighs.Add(symbol, security.Holdings.AveragePrice); // Set to average holding cost
                        continue;
                    }

                    // Check for new highs and update - set to tradebar high
                    if (_trailingHighs[symbol] < security.High)
                    {
                        _trailingHighs[symbol] = security.High;
                        continue;
                    }

                    // Check for securities past the drawdown limit
                    var securityHigh = _trailingHighs[symbol];
                    var drawdown = (security.Low / securityHigh) - 1m;
                    var profit = (security.High / security.Holdings.AveragePrice) - 1m;

                    if (drawdown < _maximumDrawdownPercent || profit >= _minimumProfitTakerPercent)
                    {
                        // liquidate
                        yield return new PortfolioTarget(security.Symbol, 0);
                    }
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$74000.00"},
            {"Lowest Capacity Asset", "AAPL 2ZQGWTSSZ0WLI|AAPL R735QTJ8XC9X"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0.327"},
            {"Kelly Criterion Probability Value", "1"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "26"},
            {"Total Insights Closed", "24"},
            {"Total Insights Analysis Completed", "24"},
            {"Long Insight Count", "26"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$31.01809"},
            {"Total Accumulated Estimated Alpha Value", "$1.89555"},
            {"Mean Population Estimated Insight Value", "$0.07898125"},
            {"Mean Population Direction", "50%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "50.0482%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "ce06ddfa4b2ffeb666a8910ac8836992"}
        };

        public long DataPoints => throw new NotImplementedException();

        public int AlgorithmHistoryDataPoints => throw new NotImplementedException();
    }
}
