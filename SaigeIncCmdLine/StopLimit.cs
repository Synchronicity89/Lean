using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaigeIncCmdLine
{
    //public partial class BootCampTask1 : QCAlgorithm
    //{
    //    private MomentumPercent spyMomentum;
    //    //Order ticket for our stop order, Datetime when stop order was last hit
    //    private OrderTicket stopMarketTicket;
    //    private DateTime stopMarketOrderFilled;
    //    private decimal highestSPYPrice;

    //    public override void Initialize()
    //    {
    //        SetStartDate(2018, 12, 1);
    //        SetEndDate(2018, 12, 10);
    //        SetCash(100000);
    //        var spy = AddEquity("SPY", Resolution.Daily);
    //        spy.SetDataNormalizationMode(DataNormalizationMode.Raw);

    //        spyMomentum = MOMP("SPY", 50, Resolution.Daily);

    //        //SetBenchmark("SPY");
    //        SetWarmUp(50);
    //    }

    //    public override void OnData(Slice slice)
    //    {
    //        if (IsWarmingUp)
    //            return;
    //        if ((Time - stopMarketOrderFilled).Days < 15)
    //            return;

    //        if (!Portfolio.Invested)
    //        {
    //            MarketOrder("SPY", 500);
    //            highestSPYPrice = Securities["SPY"].Close;
    //            stopMarketTicket = StopMarketOrder("SPY", -500, highestSPYPrice * 0.9m);

    //        }
    //        else
    //        {
    //            Plot("Levels", "Asset Price", Securities["SPY"].Price);
    //            Plot("Levels", "Stop Price", Securities["SPY"].Price * 0.9m);
    //            //1. Check if the SPY price is higher that highestSPYPrice.
    //            if (Securities["SPY"].Close > highestSPYPrice)
    //            {
    //                //2. Save the new high to highestSPYPrice; then update the stop price to 90% of highestSPYPrice 
    //                highestSPYPrice = Securities["SPY"].Close;
    //                stopMarketTicket = StopMarketOrder("SPY", -500, highestSPYPrice * 0.9m);
    //                //3. Print the new stop price with Debug()
    //                //Debug(highestSPYPrice * 0.9m);
    //            }
    //        }
    //    }

    //    public override void OnOrderEvent(OrderEvent orderEvent)
    //    {
    //        //Only act on fills (ignore submits)
    //        if (orderEvent.Status != OrderStatus.Filled)
    //            return;

    //        //Check if we hit our stop loss
    //        if (stopMarketTicket != null && orderEvent.OrderId == stopMarketTicket.OrderId)
    //        {
    //            stopMarketOrderFilled = Time;
    //        }
    //    }

    //}
    public partial class BootCampTask : QCAlgorithm
    {
        TradeBar openingBar;
        TradeBar currentBar;
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
            AddEquity("TSLA", Resolution.Minute);
            // 1. Create our consolidator with a TimeSpan of 30 min
            //Consolidate( "TSLA", TimeSpan.FromMinutes(30), OnDataConsolidated);

        }

        public override void OnData(Slice slice)
        {
            //1. Plot the current SPY price to "Data Chart" on series "Asset Price"

            if ((Time - stopMarketOrderFilled).TotalDays < 15)
                return;

            if (!Portfolio.Invested)
            {

                MarketOrder("SPY", 500);
                highestSPYPrice = Securities["SPY"].Close;
                stopMarketTicket = StopMarketOrder("SPY", -500, highestSPYPrice * 0.9m);

            }
            else
            {

                //2. Plot the moving stop price on "Data Chart" with "Stop Price" series name
                // You can plot multiple series on the same chart.

                if (Securities["SPY"].Close > highestSPYPrice)
                {
                    highestSPYPrice = Securities["SPY"].Close;

                    stopMarketTicket.Update(new UpdateOrderFields()
                    {
                        StopPrice = 0.9m * highestSPYPrice
                    });
                }
            }

            //1. If we have invested, or if the openingBar is null, return
            if (Portfolio.Invested == true || openingBar == null)
            {
                return;
            }

            //2. Check if the close price is above the high price, if so go 100% long on TSLA
            if (slice["TSLA"].Close > openingBar.High)
            {
                SetHoldings("TSLA", 1);
            }

            //3. Check if the close price is below the low price, if so go 100% short on TSLA
            if (slice["TSLA"].Close < openingBar.Low)
            {
                SetHoldings("TSLA", -1);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
                return;

            if (stopMarketTicket != null && orderEvent.OrderId == stopMarketTicket.OrderId)
            {
                stopMarketOrderFilled = Time;
            }
        }

        //2. Create a function OnDataConsolidator which saves the currentBar as bar 
        // Consolidators require an event handler function to recieve data
        void OnDataConsolidated(TradeBar bar)
        {
            // We can save the first bar as the currentBar
            currentBar = bar;
            if (bar.Time.Hour == 9 && bar.Time.Minute == 30)
            {
                //2. Save first bar as openingBar
                openingBar = bar;
            }
        }
    }
}
