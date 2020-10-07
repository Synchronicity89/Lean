
namespace QuantConnect
{
    using QuantConnect.Data.Custom.CBOE;
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

    public class CboeVixAlphaModel : AlphaModel
	{
		private Symbol _vix;
		
		public CboeVixAlphaModel(QCAlgorithm algorithm)
		{
			_vix = algorithm.AddData<CBOE>("VIX").Symbol;
		}
		
		public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
		{
			var insights = new List<Insight>();
			
			if (!data.ContainsKey(_vix))
			{
				return insights;
			}
			
			var vix = data.Get<CBOE>(_vix);
			
			// The Cboe Volatility Index® (VIX® Index) is the most popular benchmark index to measure
	        // the market’s expectation of future volatility. The VIX Index is based on
	        // options of the S&P 500® Index, considered the leading indicator of the broad
	        // U.S. stock market. The VIX Index is recognized as the world’s premier gauge
	        // of U.S. equity market volatility.
	        
	        // Generate Insights here!
			
			return insights;
		}
		
		public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            // For instruction on how to use this method, please visit
            // https://www.quantconnect.com/docs/algorithm-framework/alpha-creation#Alpha-Creation-Good-Design-Patterns
        }
	}
}