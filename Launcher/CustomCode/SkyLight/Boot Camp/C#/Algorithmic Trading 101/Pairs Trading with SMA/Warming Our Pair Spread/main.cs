namespace QuantConnect
{
    using System.Reflection;
    using QuantConnect.Algorithm;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Indicators;
    using QuantConnect.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using QuantConnect.Securities;

    public partial class WarmingOurPairSpread : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 7, 1);  
            SetEndDate(2019, 3, 31);  
            SetCash(100000);  
			var symbols = new [] {QuantConnect.Symbol.Create("PEP", SecurityType.Equity, Market.USA), QuantConnect.Symbol.Create("KO", SecurityType.Equity, Market.USA)};
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));
			UniverseSettings.Resolution = Resolution.Hour;
			UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            AddAlpha(new PairsTradingAlphaModel());
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel()); 
            SetExecution(new ImmediateExecutionModel());
        }
        public override void OnEndOfDay(Symbol symbol)
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            Log("Taking a position of " + Portfolio[symbol].Quantity.ToString() + " units of symbol " + symbol.ToString());
#pragma warning restore CA1305 // Specify IFormatProvider
        }
        
    }
    
	public partial class PairsTradingAlphaModel : AlphaModel
    {
    	SimpleMovingAverage spreadMean;
    	StandardDeviation spreadStd;
    	TimeSpan period;
    	public Security[] Pair;
    	
    	public PairsTradingAlphaModel()
        {
        	spreadMean = new SimpleMovingAverage(500);
            spreadStd = new StandardDeviation(500);
            period = TimeSpan.FromHours(2);
            Pair = new Security[2];
        }
        
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data) 
        {
        	var spread = Pair[1].Price - Pair[0].Price;
	        spreadMean.Update(algorithm.Time, spread);
	        spreadStd.Update(algorithm.Time, spread);
	        
	        var upperthreshold = spreadMean + spreadStd;
			var lowerthreshold = spreadMean - spreadStd;
			
	        if (spread > upperthreshold)
	        {
            	return Insight.Group( 
                    Insight.Price(Pair[0].Symbol, period, InsightDirection.Up),
                    Insight.Price(Pair[1].Symbol, period, InsightDirection.Down)
                );
            }
            
            if (spread < lowerthreshold)
            {
            	return Insight.Group( 
                	Insight.Price(Pair[0].Symbol, period, InsightDirection.Down), 
                    Insight.Price(Pair[1].Symbol, period, InsightDirection.Up) 
                );
            }

			return Enumerable.Empty<Insight>();
        }
        
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {	
            Pair = changes.AddedSecurities.ToArray();
            
            
            //1. Call for 500 days of history data for each symbol in the pair and save to the variable history
			var history = algorithm.History(Pair.Select(x=>x.Symbol), 500);
	        
	        //2. Iterate through the history tuple and update the mean and standard deviation with historical data 
			foreach(var slice in history)
			{
				var spread = slice[Pair[1].Symbol].Close - slice[Pair[0].Symbol].Close;
    
			    spreadMean.Update(slice.Time, spread);
			    spreadStd.Update(slice.Time, spread);
			}

        }
    }
}