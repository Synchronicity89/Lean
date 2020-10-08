using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Alpha model that uses an EMA cross to create insights
    /// </summary>
    public class WeightedEmaCrossAlphaModel : AlphaModel
    {
        private readonly int _fastPeriod;
        private readonly int _slowPeriod;
        private readonly Resolution _resolution;
        private readonly int _predictionInterval;
        private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmaCrossAlphaModel"/> class
        /// </summary>
        /// <param name="fastPeriod">The fast EMA period</param>
        /// <param name="slowPeriod">The slow EMA period</param>
        /// <param name="resolution">The resolution of data sent into the EMA indicators</param>
        public WeightedEmaCrossAlphaModel(
            int fastPeriod = 12,
            int slowPeriod = 26,
            Resolution resolution = Resolution.Daily
            )
        {
            _fastPeriod = fastPeriod;
            _slowPeriod = slowPeriod;
            _resolution = resolution;
            _predictionInterval = fastPeriod;
            _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
            Name = $"{nameof(EmaCrossAlphaModel)}({fastPeriod},{slowPeriod},{resolution})";
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();
            foreach (var symbolData in _symbolDataBySymbol.Values)
            {
                if (symbolData.Fast.IsReady && symbolData.Slow.IsReady)
                {
                    var ratio1 = (double)symbolData.Slow.Current.Value / (double)symbolData.Fast.Current.Value;
                    var ratio2 = (double)symbolData.Fast.Current.Value / (double)symbolData.Slow.Current.Value;
                    var insightPeriod = _resolution.ToTimeSpan().Multiply(_predictionInterval);
                    if (symbolData.FastIsOverSlow)
                    {
                        if (ratio1 > 1.0005)
                        {
                            insights.Add(Insight.Price(
                                symbolData.Symbol,
                                _resolution,
                                _predictionInterval,
                                InsightDirection.Down,
                                null, null, null,
                                ratio1
                            ));
                        }
                    }
                    else if (symbolData.SlowIsOverFast)
                    {
                        if (ratio2 > 1.0005)
                        {
                            insights.Add(Insight.Price(
                                symbolData.Symbol,
                                _resolution,
                                _predictionInterval,
                                InsightDirection.Up,
                                null, null, null,
                                ratio2
                            ));
                        }
                    }
                }

                symbolData.FastIsOverSlow = symbolData.Fast > symbolData.Slow;
            }

            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                SymbolData symbolData;
                if (!_symbolDataBySymbol.TryGetValue(added.Symbol, out symbolData))
                {
                    // create fast/slow EMAs
                    var fast = algorithm.EMA(added.Symbol, _fastPeriod, _resolution);
                    var slow = algorithm.EMA(added.Symbol, _slowPeriod, _resolution);
                    _symbolDataBySymbol[added.Symbol] = new SymbolData
                    {
                        Security = added,
                        Fast = fast,
                        Slow = slow
                    };
                }
                else
                {
                    // a security that was already initialized was re-added, reset the indicators
                    symbolData.Fast.Reset();
                    symbolData.Slow.Reset();
                }
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        private class SymbolData
        {
            public Security Security { get; set; }
            public Symbol Symbol => Security.Symbol;
            public ExponentialMovingAverage Fast { get; set; }
            public ExponentialMovingAverage Slow { get; set; }

            /// <summary>
            /// True if the fast is above the slow, otherwise false.
            /// This is used to prevent emitting the same signal repeatedly
            /// </summary>
            public bool FastIsOverSlow { get; set; }
            public bool SlowIsOverFast => !FastIsOverSlow;
        }
    }
}