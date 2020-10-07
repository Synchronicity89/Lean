namespace QuantConnect
{
    using QuantConnect.Algorithm;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Indicators;
    using QuantConnect.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    public partial class TimeOurMarketMoves : QCAlgorithm
    {

        private MomentumPercent spyMomentum;
        private MomentumPercent bondMomentum;
        public override void Initialize()
        {
            SetStartDate(2007, 8, 1);  
            SetEndDate(2010, 8, 1);  
            SetCash(3000); 

			AddEquity("SPY", Resolution.Daily);  
			AddEquity("BND", Resolution.Daily);  

			spyMomentum = MOMP("SPY", 50, Resolution.Daily);  
			bondMomentum = MOMP("BND", 50, Resolution.Daily);			
			
			SetBenchmark("SPY");  
			SetWarmUp(50); 
        }

        public override void OnData(Slice data)
        {
        	if (IsWarmingUp)
        		return;
        	
        	//1. Limit trading to happen once per week
        	if (Time.DayOfWeek != DayOfWeek.Tuesday)
            {
            	return;
            }
            
            if (spyMomentum > bondMomentum)
            {
            	Liquidate("BND");
            	SetHoldings("SPY", 1);
            }
            
            else
            {
            	Liquidate("SPY");
            	SetHoldings("BND", 1);
            }
        }
        
    }
}