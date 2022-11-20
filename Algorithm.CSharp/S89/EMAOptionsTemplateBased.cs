namespace QuantConnect.Algorithm.CSharp.S89
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Indicators;
    using QuantConnect.Interfaces;
    using QuantConnect.Securities;
    using QuantConnect.Securities.Option;

    /// <summary>
    /// Basic template options framework algorithm uses framework components to define an algorithm
    /// that trades options.
    /// </summary>
    public class EMAOptionsTemplateBased : QCAlgorithm
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2019, 12, 21);
            SetEndDate(2020, 07, 06);
            SetCash(100000);

            // set framework models
            SetUniverseSelection(new EarliestExpiringWeeklyAtTheMoneyOptionUniverseSelectionModel(SelectOptionChainSymbols));
            //SetAlpha(new ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromHours(0.5)));
            SetAlpha(new ConstantOptionContractAlphaModel());
            SetPortfolioConstruction(new SingleSharePortfolioConstructionModel());

            //SetExecution(new ImmediateExecutionModel());
            //SetExecution(new VolumeWeightedAveragePriceExecutionModel());
            SetExecution(new StandardDeviationExecutionModel(30, 1.15m, Resolution.Hour)); 
            SetRiskManagement(new TrailingStopProfitTakerRiskManagementModel(0.15m, 0.30m));
        }

        // option symbol universe selection function
        private static IEnumerable<Symbol> SelectOptionChainSymbols(DateTime utcTime)
        {
            yield return QuantConnect.Symbol.Create("SPY", SecurityType.Option, Market.USA, "?SPY");
            //var newYorkTime = utcTime.ConvertFromUtc(TimeZones.NewYork);
            //if (newYorkTime.Date < new DateTime(2014, 06, 06))
            //{
            //    yield return QuantConnect.Symbol.Create("TWX", SecurityType.Option, Market.USA, "?TWX");
            //}

            //if (newYorkTime.Date >= new DateTime(2014, 06, 06))
            //{
            //    yield return QuantConnect.Symbol.Create("AAPL", SecurityType.Option, Market.USA, "?AAPL");
            //}
        }

        /// <summary>
        /// Creates option chain universes that select only the earliest expiry ATM weekly put contract
        /// and runs a user defined optionChainSymbolSelector every day to enable choosing different option chains
        /// </summary>
        class EarliestExpiringWeeklyAtTheMoneyOptionUniverseSelectionModel : OptionUniverseSelectionModel
        {
            public EarliestExpiringWeeklyAtTheMoneyOptionUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector)
                : base(TimeSpan.FromDays(1), optionChainSymbolSelector)
            {
            }

            /// <summary>
            /// Defines the option chain universe filter
            /// </summary>
            protected override OptionFilterUniverse Filter(OptionFilterUniverse filter)
            {
                return filter
                    .Strikes(+1,+1)
                    // Expiration method accepts TimeSpan objects or integer for days.
                    // The following statements yield the same filtering criteria
                    .Expiration(3, 4)
                    //.Expiration(TimeSpan.Zero, TimeSpan.FromDays(7))
                    .IncludeWeeklys()
//                    .PutsOnly()
                    .OnlyApplyFilterAtMarketOpen();
            }
        }

        /// <summary>
        /// Implementation of a constant alpha model that only emits insights for option symbols
        /// </summary>
        //class ConstantOptionContractAlphaModel : ConstantAlphaModel
        class ConstantOptionContractAlphaModel : IntradayOptionReversalAlphaModel
        {
            //public ConstantOptionContractAlphaModel(InsightType type, InsightDirection direction, TimeSpan period)
            //    : base(type, direction, period)
            //{
            //}
            public ConstantOptionContractAlphaModel()
            : base()
            {
            }

            //protected override bool ShouldEmitInsight(DateTime utcTime, Symbol symbol)
            protected override bool ShouldEmitInsight(QCAlgorithm algorithm, Symbol symbol)
            {
                // only emit alpha for option symbols and not underlying equity symbols
                if (symbol.SecurityType != SecurityType.Option)
                {
                    return false;
                }

                //return base.ShouldEmitInsight(utcTime, symbol);
                return base.ShouldEmitInsight(algorithm, symbol);
            }
            //Symbol currentCall;
            //Symbol currentPut;
            //protected override bool ShouldEmitInsight(QCAlgorithm algorithm, Symbol symbol)
            //{
            //    Option option;
            //    // only emit alpha for option symbols and not underlying equity symbols
            //    if (symbol.SecurityType != SecurityType.Option)
            //    {
            //        return false;
            //    }
            //    if (currentCall != null && algorithm.Securities.ContainsKey(currentCall) == false)
            //    {
            //        currentCall = null;
            //    }

            //    if (currentPut != null && algorithm.Securities.ContainsKey(currentPut) == false)
            //    {
            //        currentPut = null;
            //    }

            //    option = (Option)algorithm.ActiveSecurities[symbol];
            //    if (option.Right == OptionRight.Call && currentCall == null)
            //    {
            //        currentCall = symbol;
            //    }
            //    if (option.Right == OptionRight.Put && currentPut == null)
            //    {
            //        currentPut = symbol;
            //    }

            //    return base.ShouldEmitInsight(algorithm, option.Right == OptionRight.Call ? currentCall : currentPut);
            //}
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

    public class IntradayOptionReversalAlphaModel : AlphaModel
    {
        private readonly int _periodSma;
        private readonly Resolution _resolution;
        private readonly Dictionary<Symbol, SymbolData> _cache;

        public IntradayOptionReversalAlphaModel(
            int periodSma = 5,
            Resolution resolution = Resolution.Hour)
        {
            _periodSma = periodSma;
            _resolution = resolution;
            _cache = new Dictionary<Symbol, SymbolData>();
            Name = "IntradayReversalAlphaModel";
        }

        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            // Set the time to close all positions at 3PM
            var timeToClose = algorithm.Time.Date.Add(new TimeSpan(0, 15, 1, 0));

            var insights = new List<Insight>();

            foreach (var kvp in algorithm.ActiveSecurities)
            {
                var symbol = kvp.Key;

                SymbolData symbolData;

                if (ShouldEmitInsight(algorithm, symbol) &&
                    _cache.TryGetValue(symbol, out symbolData))
                {
                    var price = kvp.Value.Price;

                    Option option = (Option)kvp.Value;
                    var direction = symbolData.IsUptrend(price)
                        ? InsightDirection.Up
                        : InsightDirection.Down;

                    // Ignore signal for same direction as previous signal (when no crossover)
                    if (direction == symbolData.PreviousDirection)
                    {
                        continue;
                    }

                    // Save the current Insight Direction to check when the crossover happens
                    symbolData.PreviousDirection = direction;

                    // Generate insight
                    insights.Add(Insight.Price(symbol, timeToClose, direction));
                }
            }

            return insights;
        }

        protected virtual bool ShouldEmitInsight(QCAlgorithm algorithm, Symbol symbol)
        {
            var timeOfDay = algorithm.Time.TimeOfDay;

            return algorithm.Securities[symbol].HasData &&
                timeOfDay >= TimeSpan.FromHours(10) &&
                timeOfDay <= TimeSpan.FromHours(15);
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var symbol in changes.AddedSecurities.Select(x => x.Symbol))
            {
                if (_cache.ContainsKey(symbol)) continue;
                _cache.Add(symbol, new SymbolData(algorithm, symbol, _periodSma, _resolution));
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        private class SymbolData
        {
            private readonly SimpleMovingAverage _priceSMA;

            public InsightDirection PreviousDirection { get; set; }

            public SymbolData(QCAlgorithm algorithm, Symbol symbol, int periodSma, Resolution resolution)
            {
                PreviousDirection = InsightDirection.Flat;
                _priceSMA = algorithm.SMA(symbol, periodSma, resolution);
            }

            public bool IsUptrend(decimal price)
            {
                return _priceSMA.IsReady && price < Math.Round(_priceSMA * 1.001m, 6);
            }
        }
    }

    public class TrailingStopProfitTakerRiskManagementModel : RiskManagementModel
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
}

