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
    public partial class SchedulingEvents : QCAlgorithm
    {
    	TradeBar openingBar;
    	
        public override void Initialize()
        {
            SetStartDate(2018, 7, 1); 
            SetEndDate(2019, 7, 1); 
            SetCash(100000);  
            AddEquity("TSLA", Resolution.Minute);
            Consolidate("TSLA", TimeSpan.FromMinutes(30), OnDataConsolidated);
        	
        	//3. Created a scheduled event triggered at 1:30 calling the ClosePositions function
        	// Coordinate algorithm activities with Schedule.On()
			Schedule.On(DateRules.EveryDay("TSLA"), TimeRules.At(13,30), ClosePositions);
        }
        //1. Create a function named void ClosePositions()
        //2. Set openingBar to null and liquidate TSLA
        void ClosePositions()
		{
		    openingBar = null;
		    Liquidate("TSLA");
		}
        

        public override void OnData(Slice data) 
        {
        	if (Portfolio.Invested || openingBar == null){
        		return;
        	}
        	
        	if (data["TSLA"].Close > openingBar.High){
        		SetHoldings("TSLA", 1);
        	}
        	
        	if (data["TSLA"].Close < openingBar.Low){
        		SetHoldings("TSLA", -1);
        	}
        }
        
        private void OnDataConsolidated(TradeBar bar)
        {
        	if (bar.Time.Hour == 9 && bar.Time.Minute == 30)
        	{
        		openingBar = bar;
        	}
        }
         
    }
}