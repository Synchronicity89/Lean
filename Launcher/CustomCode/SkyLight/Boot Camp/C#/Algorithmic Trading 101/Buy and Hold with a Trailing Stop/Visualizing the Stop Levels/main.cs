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

    public partial class VisualizingtheStopLevels : QCAlgorithm
    {
        private OrderTicket stopMarketTicket; 
        private DateTime stopMarketOrderFilled;
        private decimal highestSPYPrice = Decimal.MinValue;

        public override void Initialize()
        {
            SetStartDate(2018, 12, 1);
            SetEndDate(2019, 4, 1);
            SetCash(100000);
            var spy = AddEquity("SPY", Resolution.Daily);
            spy.SetDataNormalizationMode(DataNormalizationMode.Raw);
        }

        public override void OnData(Slice slice)
        {
        	//1. Plot the current SPY price to "Data Chart" on series "Asset Price"
        	Plot("Levels", "Asset Price", Securities["SPY"].Price);
            Plot("Levels", "Stop Price", Securities["SPY"].Price * 0.9m);
            if ((Time - stopMarketOrderFilled).TotalDays < 15)
                return;

            if (!Portfolio.Invested) {

                MarketOrder("SPY", 500);
                highestSPYPrice = Securities["SPY"].Close;
                stopMarketTicket = StopMarketOrder("SPY", -500, highestSPYPrice * 0.9m);
                
            } else {
            	
            	//2. Plot the moving stop price on "Data Chart" with "Stop Price" series name
            	
				if (Securities["SPY"].Close > highestSPYPrice) {
					highestSPYPrice = Securities["SPY"].Close;
					
					stopMarketTicket.Update(new UpdateOrderFields() { 
						StopPrice = 0.9m * highestSPYPrice
					});
				}
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        { 
            if (orderEvent.Status != OrderStatus.Filled)
                return;

            if (stopMarketTicket != null && orderEvent.OrderId == stopMarketTicket.OrderId) {
                stopMarketOrderFilled = Time;
            }
        }
        
    }
}