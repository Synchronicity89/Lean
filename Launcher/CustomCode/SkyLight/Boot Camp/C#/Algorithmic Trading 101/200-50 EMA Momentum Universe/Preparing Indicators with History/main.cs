
namespace QuantConnect
{
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
    using System.Collections.Concurrent;
    using QuantConnect.Indicators;

    public partial class BootCampTask4 : QCAlgorithm
    {
        private readonly ConcurrentDictionary<Symbol, SelectionData> averages = new ConcurrentDictionary<Symbol, SelectionData>(); 
        
        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);  
            SetEndDate(2019, 4, 1);  
            SetCash(100000); 
			AddUniverse(CoarseSelectionFilter);
			UniverseSettings.Resolution = Resolution.Daily;
        }
		
		public IEnumerable<Symbol> CoarseSelectionFilter(IEnumerable<CoarseFundamental> universe)
    	{
    		var selected = new List<Symbol>();
			universe = universe
	            .Where(x => x.Price > 10)
	            .OrderByDescending(x => x.DollarVolume).Take(100);
       
    		foreach (var coarse in universe)
    		{
    			var symbol = coarse.Symbol;
    			 
    			if (!averages.ContainsKey(symbol)) 
    			{
        			//1. Call history to get an array of 200 days of history data
                    var history = History(symbol, 200, Resolution.Daily);
                    
                    //2. Adjust SelectionData to pass in the history result
                    averages[symbol] = new SelectionData(history);
    			}
    			
        		averages[symbol].Update(Time, coarse.AdjustedPrice); 
        		 
        		if (averages[symbol].IsReady() && averages[symbol].Fast > averages[symbol].Slow)
        		{
    				selected.Add(coarse.Symbol);
        		}
    		}
    		
        	return selected.Take(10);
        }
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities)
            {	
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }
            
            foreach (var security in changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 0.10m);
            }
        }
    }
    
    public partial class SelectionData 
    {
        public readonly ExponentialMovingAverage Fast;
        public readonly ExponentialMovingAverage Slow;
        public bool IsReady() {return Slow.IsReady && Fast.IsReady;}
		
		//3. Update the constructor to accept an IEnumerable<TradeBar> history parameter
    	public SelectionData(IEnumerable<TradeBar> history)
        {	
            Fast = new ExponentialMovingAverage(50);  
            Slow = new ExponentialMovingAverage(200);
            
            //4. Loop over history data and pass the bar.EndTime and bar.Close values to Update()
            foreach(var bar in history)
			{
			    Update(bar.EndTime, bar.Close);
			}
        }
        
        public bool Update(DateTime time, decimal value)
        {
            Slow.Update(time, value);
            Fast.Update(time, value);
            return IsReady();
        }
    }
}