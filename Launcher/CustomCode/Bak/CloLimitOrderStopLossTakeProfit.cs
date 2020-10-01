using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Lean.Launcher.CustomCode
{
    //    class OrderState :
    //      # limitTicket = None
    //      # stopLossTicket = None
    //      # tpTicket = None
    //        R=0.005
    //        M=0.02
    //        limitFilled = False
    //        bullBear = 0 # -1 for bear, 1 for bull
    //      symb = ""
    //        algo = None
    //        def __init__(self, algorithm, _symb, _bullBear, limitR, stopM):
    //            self.symb = _symb
    //            self.algo = algorithm
    //            self.R = limitR
    //            self.M = stopM
    //            self.bullBear = _bullBear
    //        def OnOrderEvent(self, orderEvent):
    //            replace = False
    //        if orderEvent.Status != OrderStatus.Filled:
    //            return
    //        # Filled order is main Limit Order
    //        self.algo.Debug("orderEvent filled id " + str(orderEvent.OrderId))
    //        if self.algo.tpOrder is None and self.algo.limitOrder is not None and self.algo.limitOrder.OrderId == orderEvent.OrderId:

    //                self.limitTicket = self.algo.Transactions.GetOrderTicket(orderEvent.OrderId)

    //            # create take profit order created
    //            self.algo.tpOrder = self.algo.LimitOrder(self.S, -self.limitTicket.QuantityFilled, self.limitTicket.AverageFillPrice* (1.00 + self.R))

    //                self.algo.Debug("tpOrder created id = " + str(self.algo.tpOrder.OrderId) + " AvgFP:" + str(self.limitTicket.AverageFillPrice))
    //            if self.algo.highestSPYPrice == -1:

    //                    self.algo.highestSPYPrice = self.algo.CurrentSlice[self.S].Close

    //            # stop market order created
    //            self.algo.stopLossOrder = self.algo.stopLossOrder(self.S, -self.A, self.algo.highestSPYPrice* (1.00 - self.M))

    //                self.algo.limitOrder = None

    //                replace = False
    //            return replace

    //        # Filled order is take profit Order
    //        if self.algo.tpOrder is not None and self.algo.tpOrder.OrderId == orderEvent.OrderId:

    //                self.algo.Debug("tpOrder set None = " + str(self.algo.tpOrder.OrderId))

    //                self.algo.tpOrder = None
    //            if self.algo.stopLossOrder is not None:

    //                    self.algo.stopLossOrder.Cancel("Cancelling stop loss due to take profit being filled")

    //                    self.algo.stopLossOrder = None

    //                replace = True
    //            return replace

    //        # Filled order is stop market Order
    //        if self.algo.stopLossOrder is not None and self.algo.stopLossOrder is not None and self.algo.stopLossOrder.OrderId == orderEvent.OrderId: 

    //                self.stopMarketOrderFillTime = self.Time
    //            if self.algo.tpOrder is not None:

    //                    self.algo.tpOrder.Cancel("Take profit cancelled due to stop loss id: " + str(orderEvent.OrderId))

    //                    self.algo.tpOrder = None

    //                self.algo.Debug("stopLossOrder time set = " + str(orderEvent.OrderId))

    //                replace = True
    //            return replace

    public class OrderState
    {
        private string ticker;
        public decimal limitOffset = 0.03M;
        public decimal stopOffset = 0.10M;
        public OrderTicket limitOrder;
        public OrderTicket slOrder;
        public OrderTicket tpOrder;
        public decimal highestSPYPrice;
        public int bullBear = 1;
        public LimitOrderStopLossTakeProfit algo;
        private DateTime stopMarketOrderFillTime;

        public OrderState(string _ticker)
        {
            this.ticker = _ticker;
        }

        public OrderState(string _ticker, LimitOrderStopLossTakeProfit algorithm, int _bullBear, decimal _limitOffset, decimal _stopOffset) : this(_ticker)
        {
            algo = algorithm;
            limitOffset = _limitOffset;
            stopOffset = _stopOffset;
            bullBear = _bullBear;
        }

        public bool OnOrderEvent(OrderEvent orderEvent)
        {
            // Limit Order has been filled, so now create Take Profit and Stop Loss orders
            if (tpOrder == null && limitOrder != null && limitOrder.OrderId == orderEvent.OrderId)
            {
                // Get the Limit Ticket
                var limitTicket = algo.Transactions.GetOrderTicket(orderEvent.OrderId);

                // Create Take Profit Order of opposite amount
                tpOrder = algo.LimitOrder(ticker, -limitTicket.QuantityFilled * bullBear, limitTicket.AverageFillPrice * (1.00M + bullBear * stopOffset * 1.5M));
                //                self.algo.Debug("tpOrder created id = " + str(self.algo.tpOrder.OrderId) + " AvgFP:" + str(self.limitTicket.AverageFillPrice))
                //            if self.algo.highestSPYPrice == -1:
                if (highestSPYPrice == -1)
                {
                    //                    self.algo.highestSPYPrice = self.algo.CurrentSlice[self.S].Close
                    highestSPYPrice = algo.CurrentSlice[ticker].Close;
                }
                //            # stop market order created
                //            self.algo.stopLossOrder = self.algo.stopLossOrder(self.S, -self.A, self.algo.highestSPYPrice* (1.00 - self.M))
                slOrder = algo.StopMarketOrder(ticker, -limitTicket.QuantityFilled * bullBear, highestSPYPrice * (1.00M - stopOffset * bullBear));
                limitOrder = null;
                return false;
            }
            //        # Filled order is take profit Order
            //        if self.algo.tpOrder is not None and self.algo.tpOrder.OrderId == orderEvent.OrderId:
            if (tpOrder != null && tpOrder.OrderId == orderEvent.OrderId)
            {
                tpOrder = null;
                //            if self.algo.stopLossOrder is not None:
                if (slOrder != null)
                {
                    //                    self.algo.stopLossOrder.Cancel("Cancelling stop loss due to take profit being filled")
                    slOrder.Cancel("Cancelling stop loss due to take profit being filled");
                    //                    self.algo.stopLossOrder = None
                    slOrder = null;
                }
                return true;
            }

            //        # Filled order is stop market Order
            //        if self.algo.stopLossOrder is not None and self.algo.stopLossOrder is not None and self.algo.stopLossOrder.OrderId == orderEvent.OrderId: 
            if (slOrder != null && slOrder.OrderId == orderEvent.OrderId)
            {
                stopMarketOrderFillTime = algo.Time;
                //            if self.algo.tpOrder is not None:
                if (tpOrder != null)
                {
                    //                    self.algo.tpOrder.Cancel("Take profit cancelled due to stop loss id: " + str(orderEvent.OrderId))
                    tpOrder.Cancel("Take profit cancelled due to stop loss id: " + orderEvent.OrderId.ToString(new CultureInfo("en-us")));
                    //                    self.algo.tpOrder = None
                    tpOrder = null;
                }
                return true;
            }
            return false;
        }
    }

    public class LimitOrderStopLossTakeProfit : QCAlgorithm
    {
        List<string> tickers = new List<string>(new string[] { "SPY", "BND" });
        Dictionary<string, OrderState> orders = new Dictionary<string, OrderState>();
        public override void Initialize()
        {
            SetStartDate(2015, 1, 1);
            SetEndDate(2020, 1, 1);
            SetCash(100000);
            tickers.ForEach(t =>
            {
                AddEquity(t, Resolution.Daily).SetDataNormalizationMode(DataNormalizationMode.Raw);
                orders.Add(t, new OrderState(t));
            });
        }
        //class BootCampTask(QCAlgorithm):

        //    # Order ticket for our stop order, Datetime when stop order was last hit
        //    # limitOrder = None
        //    # stopLossOrder = None
        //    # tpOrder = None
        //    limitOrder = None
        //    stopLossOrder = None
        //    tpOrder = None

        //    stopMarketOrderFillTime = datetime.min
        //    highestSPYPrice = -1
        //    S="SPY"
        //    A=500
        //    R=0.02
        //    M=0.07
        //    orderStates = {}

        //test = False

        //def Initialize(self) :
        //        self.SetStartDate(2015, 1, 1)
        //        self.SetEndDate(2020, 1, 1)
        //        self.SetCash(100000)
        //        spy = self.AddEquity(self.S, Resolution.Daily)
        //        spy.SetDataNormalizationMode(DataNormalizationMode.Raw)

        public override void OnData(Slice slice)
        {
            var self = this;
            //if (self.Time - self.stopMarketOrderFillTime).days < 15:
            //    return
            foreach (var ticker in slice.Keys.Select(k => k.Value))
            {
                if (this.Securities[ticker].Holdings.Quantity == 0)
                {
                    if (Portfolio.MarginRemaining > this.Securities[ticker].Close)
                    {
                        orders[ticker].limitOrder = LimitOrder(ticker, Portfolio.MarginRemaining / (this.Securities.Count * this.Securities[ticker].Close),
                            self.Securities[ticker].Price * (1.00M - orders[ticker].stopOffset));
                    }
                    else
                    {
                        // 5 > 4 e.g. bull, -3 > -4 e.g. bear
                        if (Securities[ticker].Close * orders[ticker].bullBear > orders[ticker].highestSPYPrice * orders[ticker].bullBear)
                        {

                            orders[ticker].highestSPYPrice = self.Securities[ticker].Close;
                            var updateFields = new UpdateOrderFields();
                            updateFields.StopPrice = orders[ticker].highestSPYPrice * (1.00M - orders[ticker].stopOffset);
                            if (orders[ticker].slOrder != null)
                                orders[ticker].slOrder.Update(updateFields);
                        }
                    }
                }

            }
        }
        //    def OnData(self, data):
        //        if not self.S in self.orderStates.keys():
        //            self.orderStates[self.S] = OrderState(self, self.S, 1, self.R, self.M)
        //        #limitOrder, when filled becomes none, 
        //        testedOK = True
        //        if self.test:
        //            if self.limitOrder is None and (self.stopLossOrder is  None or self.tpOrder is  None):
        //                testedOK = False
        //                self.Debug("********  limitOrder is None, others not set !!!!!!!!!!")
        //            if self.limitOrder is not None and(self.stopLossOrder is not None or self.tpOrder is not None) :
        //                testedOK = False
        //                self.Debug("!!!!!!!!!!  limitOrder is not None, one or two others set ********")
        //            if not testedOK:
        //                pass

        //# 1. Plot the current SPY price to "Data Chart" on series "Asset Price"
        //        self.Plot("Levels", "Asset Price", self.Securities[self.S].Price)

        //        if (self.Time - self.stopMarketOrderFillTime).days< 15:
        //            return

        //        if not self.Portfolio.Invested and self.limitOrder == None:
        //            self.orderStates[self.S] = OrderState(self, self.S, 1, self.R, self.M)
        //            self.orderStates[self.S].limitOrder = self.LimitOrder(self.S, self.A, self.Securities[self.S].Price* (1.00 - self.R))
        //            self.test = True

        //        else:

        //            #2. Plot the moving stop price on "Data Chart" with "Stop Price" series name
        //            self.Plot("Levels", "Stop Price",  self.Securities[self.S].Price* (1.00 - self.M))
        //            #self.Debug("Levels Stop Price" + str(self.Securities[self.S].Price * 0.9))
        //            if self.tpOrder is not None:
        //                self.Plot("Levels", "TP Price",  self.tpOrder.Get(OrderField.LimitPrice))
        //                self.Debug("Levels TP Price" + str(self.tpOrder.Get(OrderField.LimitPrice)))
        //            else:
        //                self.Plot("Levels", "TP Price",  0.0)
        //                self.Debug("No TP Order ***")
        //            if self.Securities[self.S].Close > self.highestSPYPrice:

        //                self.highestSPYPrice = self.Securities[self.S].Close
        //                updateFields = UpdateOrderFields()
        //                updateFields.StopPrice = self.highestSPYPrice* (1.00 - self.M)
        //                if self.stopLossOrder is not None:
        //                    self.stopLossOrder.Update(updateFields)
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            foreach (var ticker in tickers)
            {
                bool replace = orders[ticker].OnOrderEvent(orderEvent);
            }
        }
        //    def OnOrderEvent(self, orderEvent) :
        //        replace = False
        //        if self.S in self.orderStates.keys():
        //            o = self.orderStates[self.S]
        //            self.Debug(type(o))
        //            replace = o.OnOrderEvent(orderEvent)
        //        if replace:
        //            self.orderStates[self.S] = OrderState(self, self.S, 1, self.R, self.M)
    }
}
