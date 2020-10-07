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
    using QuantConnect.Indicators;
    using QuantConnect.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public partial class ReducingaParameter : QCAlgorithm
    {
    	private decimal deviations;
    	private RollingWindow<TradeBar> window;
    	private StandardDeviation volatility;
    	
        public override void Initialize()
        {
	        SetStartDate(2017, 11, 1);  
        	SetEndDate(2018, 7, 1);  
	        SetCash(100000);  
	        AddEquity("TSLA", Resolution.Minute);
	        Schedule.On(DateRules.EveryDay("TSLA"), TimeRules.BeforeMarketClose("TSLA", 0), ClosingBar); 
	        Schedule.On(DateRules.EveryDay("TSLA"), TimeRules.AfterMarketOpen("TSLA", 1), OpeningBar);
	        Schedule.On(DateRules.EveryDay("TSLA"), TimeRules.AfterMarketOpen("TSLA", 45), ClosePositions);
        	window = new RollingWindow<TradeBar>(2);
        	
	        //1. Create a manual Standard Deviation indicator to track recent volatility
	        volatility = new StandardDeviation("TSLA", 60);
        }
	    
	    public override void OnData(Slice data)
	    {
        	if (data["TSLA"] != null) 
        	{
	            //2. Update our standard deviation indicator manually with algorithm time and TSLA's close price
	            volatility.Update(Time, data["TSLA"].Close);
        	}
	    }
	    
	    public void OpeningBar()
	    {
	        if (CurrentSlice["TSLA"] != null) {
	            window.Add(CurrentSlice["TSLA"]);
	        }
	        
	        //3. Use IsReady to check if both volatility and the window are ready, or return to wait for tomorrow
	        if (!window.IsReady && volatility != null) return; 
	        
	        var delta = window[0].Open - window[1].Close;
	        
	        //4. Save an approximation of standard deviations to var deviations by dividing delta by the current volatility value:
	        //   Normally this is delta from the mean, but we'll approximate it with current value for this lesson. 
	        deviations = delta / volatility;
	        
	        //5. SetHoldings to 100% TSLA if delta's gap is less than -3 standard deviations
	        if(deviations < -3.0m)
	        {
	        	SetHoldings("TSLA", 1.0m);
	        }
	    }
	    
	    public void ClosingBar(){
	        window.Add(CurrentSlice["TSLA"]);
	    }

	    public void ClosePositions() {
	    	Liquidate();
	    }
    }
}