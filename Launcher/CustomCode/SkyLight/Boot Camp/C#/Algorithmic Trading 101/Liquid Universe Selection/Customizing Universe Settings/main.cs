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

    public partial class CustomizingUniverseSettings : QCAlgorithm
    {
        private IEnumerable<Symbol> filteredByPrice;
        private SecurityChanges _changes = SecurityChanges.None;
        
        public override void Initialize()
        {
            SetStartDate(2019, 1, 11); 
            SetEndDate(2019, 7, 1);  
            SetCash(100000);             
            AddUniverse(CoarseSelectionFilter);
            UniverseSettings.Resolution = Resolution.Daily;
            UniverseSettings.Leverage = 2.0m;
            
            //1. Set the leverage to 2 
        }
        
        public IEnumerable<Symbol> CoarseSelectionFilter(IEnumerable<CoarseFundamental> coarse)
        {	
        	var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume);
        	filteredByPrice = sortedByDollarVolume.Where(x => x.Price > 10).Select(x => x.Symbol);
        	filteredByPrice = filteredByPrice.Take(10);
        	return filteredByPrice;
        }
	
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
        	_changes = changes;
        	Log($"OnSecuritiesChanged({UtcTime}):: {changes}");
        	foreach (var security in changes.RemovedSecurities)
    		{
    			if (security.Invested)
    			{
    				Liquidate(security.Symbol);
    			}
    		}
    		
    		//2. Now that we have more leverage, set the allocation to set the allocation to 18% each instead of 10%
    		foreach (var security in changes.AddedSecurities)
    		{
    			SetHoldings(security.Symbol, 0.18m);
    		}
        }
        
    }
}