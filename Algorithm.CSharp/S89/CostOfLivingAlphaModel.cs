namespace QuantConnect.Algorithm.Framework.Alphas
{
    using QuantConnect.Data;
    using QuantConnect.Data.Custom;
    using QuantConnect.Data.UniverseSelection;
    using System;
    using System.Collections.Generic;
    public class CostOfLivingAlphaModel : AlphaModel
    {
        private readonly Resolution _resolution;
        private readonly Symbol _compensationDelta;
        private readonly Symbol _costOfLivingDelta;
        private readonly TimeSpan _insightPeriod;

        public CostOfLivingAlphaModel(
            QCAlgorithm algorithm,
            Resolution resolution = Resolution.Daily,
            int insightPeriod = 90
            )
        {
            _resolution = resolution;
            // Add quarterly compensation change data
            _compensationDelta = algorithm.AddData<QuandlCompensationData>("BLSP/PRS85006062", _resolution).Symbol;
            // Add quarterly cost of living data (Consumer Price Index)
            _costOfLivingDelta = algorithm.AddData<QuandlCostOfLivingData>("RATEINF/CPI_USA", _resolution).Symbol;
            _insightPeriod = resolution.ToTimeSpan().Multiply(insightPeriod);
        }


        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();

            // Check for all Quandl Symbols in current data Slice
            if (!data.ContainsKey(_compensationDelta))
            {
                return insights;
            }

            // Employee compensation levels and cost of living are important
            // factors in determining how much consumers will be able or want to spend on discretionary
            // items. The Consumer Price Index measures changes in the price level of a market basket of 
            // consumer goods and services purchased by households. If the cost of living and/or the 
            // inflation rate increases faster than compensation levels, this will likely put downward
            // pressure on consumer discretionary industries -- retail, automobile manufacturing, luxury/travel, etc.

            // Generate Insights here!

            return insights;
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {

        }
    }

    public class QuandlCompensationData : Quandl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuandlCompensationData"/> class.
        /// </summary>
        public QuandlCompensationData()
            // Rename the Quandl object column to the data we want, which is the column containing 
            // the yield in the CSV that our API call returns
            : base(valueColumnName: "Value")
        {
        }
    }

    public class QuandlCostOfLivingData : Quandl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuandlCostOfLivingData"/> class.
        /// </summary>
        public QuandlCostOfLivingData()
            // Rename the Quandl object column to the data we want, which is the column containing 
            // the yield in the CSV that our API call returns
            : base(valueColumnName: "Value")
        {
        }
    }
}