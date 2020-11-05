namespace QuantConnect.Algorithm.Framework.Alphas
{
    using QuantConnect.Data;
    using QuantConnect.Data.Custom;
    using QuantConnect.Data.UniverseSelection;
    using System;
    using System.Collections.Generic;

    public class EquityHighLowAlphaModel : AlphaModel
    {
        private readonly Resolution _resolution;
        private readonly TimeSpan _insightDuration;
        private readonly Symbol _highs;
        private readonly Symbol _lows;

        public EquityHighLowAlphaModel(
            QCAlgorithm algorithm,
            int insightPeriod = 1,
            Resolution resolution = Resolution.Daily
            )
        {
            _resolution = resolution;
            // Add Quandl data for the Federal Interest Rate
            _highs = algorithm.AddData<Quandl52WeekHigh>("URC/NASDAQ_52W_HI", _resolution).Symbol;
            _lows = algorithm.AddData<Quandl52WeekLow>("URC/NYSE_52W_LO", _resolution).Symbol;
            _insightDuration = resolution.ToTimeSpan().Multiply(insightPeriod);
        }


        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();

            // Check for all Quandl Symbols in current data Slice
            if (!(data.ContainsKey(_highs) && data.ContainsKey(_lows)))
            {
                return insights;
            }

            // Higher numbers of stocks at their 52-week low naturally correlate with recessions or
            // large stock market corrections, and the opposite can be said of large numbers of stocks
            // reaching their 52-week highs at the same time. When viewed like this, the spread between
            // the two numbers can tell us a lot about the overall direction of the US equities
            // market. If the spread between High and Low decreases and/or inverts, this is likely a
            // significant indicator of a bear market. This and other metrics can be used to generate
            // valuable Insights

            // More URC market data can be found at Quandl
            // https://www.quandl.com/data/URC-Unicorn-Research-Corporation

            // Generate Insights here!

            return insights;
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {

        }
    }

    public class Quandl52WeekHigh : Quandl
    {
        public Quandl52WeekHigh()
            : base(valueColumnName: "Numbers of Stocks")
        {
        }
    }

    public class Quandl52WeekLow : Quandl
    {
        public Quandl52WeekLow()
            : base(valueColumnName: "Numbers of Stocks")
        {
        }
    }
}