using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities.Equity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Lean.Launcher.CustomCode
{
    public static class C
    {
        public static readonly CultureInfo en_us = new CultureInfo("en-us");
        public const bool useTPLimit = true;
    }

    public class OrderStateBase
    {
        public string ticker;
        public Equity security;
        // used for SPY
        // public decimal limitOffset = 0.0012M;
        // public decimal stopOffset = 0.005M;
        // public const decimal ratio = 1.0m/1.0m;
        // public decimal limitOffset = 0.018M * ratio * 1.0m;
        // public decimal stopOffset = 0.075M * ratio * 1.0m;
        public decimal limitOffset = 0.018M * 1.0m;
        public decimal stopOffset = 0.075M * 1.0m;
        public OrderTicket limitOrder;
        public OrderTicket slOrder;

        public OrderTicket tpOrder;
        public decimal highestPrice;
        public int bullBear = 1;
        public LimitOrderStopLossTakeProfit algo;
        public DateTime stopMarketOrderFillTime;
        private List<string> logged = new List<string>();
        private string sameLog = "SameLog;";

        //TP must be dynamic.  Can't have a Limit order for TP
        public decimal tpquantity, tplimitPrice;
        public string tpmess;



        public virtual bool OnOrderEvent(OrderEvent orderEvent)
        {
            return true;
        }

        public void Report(string message)
        {
            // string toPrint = message + ";, $$$R$$$;, limitOrder;" + limitOrder?.ToString() + ",; tpOrder;" + tpOrder?.ToString() + 
            // ",; slOrder;" + slOrder?.ToString() + ",; highestPrice;" + highestPrice.ToString();
            // if(logged.Contains(toPrint) == false)
            // {
            // 	//algo.Debug(sameLog + "; " + toPrint);
            // 	sameLog = "SameLog; ";
            // }
            // else
            // {
            // 	sameLog += (logged.IndexOf(toPrint).ToString() + "; ");
            // }
        }
    }

    public class OrderState : OrderStateBase
    {

        public OrderState(string _ticker, Equity _security, LimitOrderStopLossTakeProfit algorithm)
        {
            ticker = _ticker;
            security = _security;
            algo = algorithm;
        }

        public OrderState(string _ticker, LimitOrderStopLossTakeProfit algorithm, int _bullBear, decimal _limitOffset, decimal _stopOffset, Equity _security) : this(_ticker, _security, algorithm)
        {
            limitOffset = _limitOffset;
            stopOffset = _stopOffset;
            bullBear = _bullBear;
        }

        public void TPLimitOrder(string _ticker, decimal _quantity, decimal _limitPrice, string _mess)
        {
            if (C.useTPLimit == true)
            {
                tpOrder = algo.LimitOrder(_ticker, _quantity, _limitPrice, _mess);
            }
            else
            {
                tpquantity = _quantity;
                tplimitPrice = _limitPrice;
                tpmess = _mess;
            }
        }

        public void TPCancel(string _mess)
        {
            if (C.useTPLimit == true && tpOrder != null)
            {
                tpOrder.Cancel(_mess);
            }
            else
            {
                tpquantity = 0.0m;
                tplimitPrice = 0.0m;
                tpmess = _mess;
            }
        }

        public override bool OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled || !algo.CurrentSlice.Where(k => k.Key.Value == ticker).Any()) return false;
            var close = "close;" + algo.CurrentSlice[ticker].Close.ToString() + ";highestPrice;" + highestPrice.ToString(C.en_us) + ";";
            // Limit Order has been filled, so now create Take Profit and Stop Loss orders
            if (slOrder == null && limitOrder != null && limitOrder.OrderId == orderEvent.OrderId)
            {
                // Get the Limit Ticket
                var limitTicket = algo.Transactions.GetOrderTicket(orderEvent.OrderId);

                // Create Take Profit Order of opposite amount
                var quantity = -algo.Securities[ticker].Holdings.Quantity * bullBear;
                var limitPrice = Math.Round(limitTicket.AverageFillPrice * (1.00m + bullBear * stopOffset * 2.0m), 2);
                if (bullBear > 0)
                {
                    limitPrice = limitPrice > highestPrice ? limitPrice : highestPrice;
                }
                else
                {
                    limitPrice = limitPrice < highestPrice ? limitPrice : highestPrice;
                }
                var mess = close + "TP; quantity;" + quantity.ToString(C.en_us) + " ;limitPrice;" + limitPrice.ToString(C.en_us);
                TPLimitOrder(ticker, quantity, limitPrice, mess);// * 1.5M
                if (highestPrice == -1)
                {
                    highestPrice = algo.CurrentSlice[ticker].Close;
                }
                var quantitySL = -algo.Securities[ticker].Holdings.Quantity * bullBear;
                var priceSL = Math.Round(highestPrice * (1.00m - stopOffset * bullBear), 2);
                var mess2 = close + "StopLoss;quantitySL;" + quantitySL.ToString(C.en_us) + ";priceSL;" + priceSL.ToString(C.en_us);
                slOrder = algo.StopMarketOrder(ticker, quantitySL, priceSL, mess2);
                limitOrder = null;
                Report("quantity;" + quantity.ToString(C.en_us) + " ;limitPrice;" + limitPrice.ToString(C.en_us));
                return false;
            }

            // Filled order is take profit Order
            // if (tpOrder != null && tpOrder.OrderId == orderEvent.OrderId)
            // {
            //     tpOrder = null;
            //     if (slOrder != null)
            //     {
            //         slOrder.Cancel(close + "Cancelling stop loss due to take profit being filled");
            //         slOrder = null;
            //     }
            //     Report(";Cancel");
            //     return true;
            // }

            //        # Filled order is stop market Order
            if (slOrder != null && slOrder.OrderId == orderEvent.OrderId)
            {
                stopMarketOrderFillTime = algo.Time;
                if ((tpquantity != 0.0m && C.useTPLimit == false) ||
                    (tpOrder != null && C.useTPLimit == true))
                {
                    // tpOrder.Cancel(close + "Take profit cancelled due to stop loss id: " + orderEvent.OrderId.ToString(C.en_us));
                    // tpOrder = null;
                    TPCancel(close + "Take profit cancelled due to stop loss id: " + orderEvent.OrderId.ToString(C.en_us));
                    //algo.Transactions.CancelOpenOrders(ticker);
                }
                return true;
            }
            //only other possibility is TP order being filled
            Report(";TakeProfit");
            if (C.useTPLimit == false)
            {
                tpquantity = 0.0m;
                tplimitPrice = 0.0m;
            }
            else
            {
                tpOrder = null;
            }
            tpmess = "";
            if (slOrder != null)
            {
                slOrder.Cancel(close + "Cancelling stop loss due to take profit being filled");
                slOrder = null;
            }
            Report(";Cancel");
            return true;
        }

    }

    public class LimitOrderStopLossTakeProfit : QCAlgorithm
    {
        //"CTSH", 	"AAPL", 	"ANSS", 	"ATVI", 	"ANET", 	"ADBE", 	"CELG", 	"BIIB", 	"GILD", 	"ABMD", 	"UNH", 	"ALXN", 	"BLK", 	"HFC", 	"KSU", 
        //public List<string> tickers = new List<string>(new string[] {"CTSH", "AAPL", "ANSS", "ANET", "ADBE", "CELG", "BIIB", "GILD", "ABMD", "UNH", "ALXN", "BLK" });//,
        public List<string> tickers = new List<string> { "GILD", "ABMD", "UNH", "ALXN", "BLK", "HFC", "KSU" }; //random stock picks. replace these with your own picks
        //"ATVI", "ADBE", "CELG", "BIIB", "GILD", "ABMD", "UNH", "ALXN", "BLK", "HFC", "KSU" });//"BND", "DVN" positive 2015 - 2020

        public Dictionary<string, OrderState> orders = new Dictionary<string, OrderState>();
        private int noOrderBarCount = 0;
        public override void Initialize()
        {
            SetStartDate(2012, 1, 1);
            //SetEndDate(2020, 10, 1);
            SetCash(100000);
            tickers.ForEach(t =>
            {
                Equity security = base.AddEquity(t, Resolution.Hour);
                // security.SetDataNormalizationMode(DataNormalizationMode.Raw);
                orders.Add(t, new OrderState(t, security, this));
            });
            SetBenchmark("SPY");
        }

        public override void OnData(Slice slice)
        {
            OnData((IEnumerable<KeyValuePair<Symbol, BaseData>>)slice);
        }
        public void OnData(IEnumerable<KeyValuePair<Symbol, BaseData>> slice)
        {
            var self = this;

            foreach (var ticker in slice.Select(k => k.Key.Value))
            {
                var close = "close;" + CurrentSlice[ticker].Close.ToString() + ";highestPrice;" + orders[ticker].highestPrice.ToString(C.en_us) + ";";
                if (this.Securities[ticker].Holdings.Quantity == 0)
                {
                    //if (Portfolio.MarginRemaining > this.Securities[ticker].Close)
                    //if (this.Securities[ticker].Holdings.Quantity != 0)
                    if (!Transactions.GetOpenOrders().Where(oo => oo.Symbol.Value.ToUpper(C.en_us) ==
                        ticker.ToUpper(C.en_us)).Any())
                    {
                        var unfilledCount
                            = orders.Where(o => o.Value.limitOrder != null && o.Value.limitOrder.Status != OrderStatus.Filled).Count();
                        if (unfilledCount > orders.Keys.Count / 2) unfilledCount = unfilledCount / 3; //make bigger bets until they are filled
                        if (unfilledCount == 0)
                        {
                            unfilledCount = 2;
                        }
                        var quantit = orders[ticker].bullBear * Portfolio.MarginRemaining /
                            (unfilledCount *
                            this.Securities[ticker].Close);
                        var pric = self.Securities[ticker].Price * (1.00M - orders[ticker].stopOffset);
                        var mess = close + "Initial LimitOrder; quantit;" +
                            quantit.ToString(C.en_us) + ";pric;" + pric.ToString(C.en_us);
                        //Transactions.CancelOpenOrders(ticker);
                        orders[ticker].limitOrder = LimitOrder(ticker, quantit, pric, mess);
                        orders[ticker].TPCancel(close + "Main Limit order submitted, so residual: ");
                        orders[ticker].highestPrice = -1;
                        if (orders[ticker].slOrder != null)
                        {
                            orders[ticker].slOrder.Cancel(close + "Cancelling stop loss due to new main limit order being placed");
                            orders[ticker].slOrder = null;
                        }
                        orders[ticker].Report(mess);
                    }
                }
                else
                {
                    //Do dynamic TP if price is right.
                    if (orders[ticker].tpquantity != 0.0m && C.useTPLimit == false)
                    {
                        if ((orders[ticker].tpquantity < 0.0m && self.Securities[ticker].Price >= orders[ticker].tplimitPrice) ||
                            (orders[ticker].tpquantity > 0.0m && self.Securities[ticker].Price < orders[ticker].tplimitPrice))
                        {
                            //self.Securities[ticker].Liquidate();
                            self.MarketOrder(self.Securities[ticker].Symbol, orders[ticker].tpquantity, false, orders[ticker].tpmess);
                            orders[ticker].TPCancel(close + "Take profit market order submitted: ");
                            Transactions.CancelOpenOrders(ticker);
                        }
                    }

                    // 5 > 4 e.g. bull, -3 > -4 e.g. bear
                    if (Securities[ticker].Close * orders[ticker].bullBear > orders[ticker].highestPrice * orders[ticker].bullBear)
                    {

                        orders[ticker].highestPrice = self.Securities[ticker].Close;
                        var updateFields = new UpdateOrderFields();
                        updateFields.StopPrice = orders[ticker].highestPrice * orders[ticker].bullBear * (1.00M - orders[ticker].stopOffset);
                        if (orders[ticker].slOrder != null)
                            orders[ticker].slOrder.Update(updateFields);
                        orders[ticker].Report("; updateFields.StopPrice;" + updateFields.StopPrice.ToString());
                    }
                    //test to see if there exists TP quantity and slOrder
                    // if ((C.useTPLimit == false && orders[ticker].tpquantity == 0.0m) || 
                    // 	(C.useTPLimit == true && orders[ticker].tpOrder == null) || 
                    // 	orders[ticker].slOrder == null)
                    // {
                    //     if (Portfolio.Invested == true && this.Securities[ticker].Holdings.Quantity != 0)
                    //     {
                    //         Liquidate(Securities[ticker].Symbol);
                    //         Transactions.CancelOpenOrders(ticker);
                    //     }
                    // }
                }
            }
            foreach (var ticker in slice.Select(k => k.Key.Value))
            {
                if (orders[ticker].limitOrder != null && orders[ticker].limitOrder.Status != OrderStatus.Filled)
                {
                    noOrderBarCount++;
                    if (noOrderBarCount > 40)
                    {
                        Liquidate(Securities[ticker].Symbol);
                        orders[ticker].limitOrder?.Cancel("No main limit order filled in 40 bars");
                        orders[ticker].slOrder?.Cancel("No main limit order filled in 40 bars");
                        Transactions.CancelOpenOrders(ticker);
                        if (C.useTPLimit == false)
                        {
                            orders[ticker].tpquantity = 0.0m;
                            orders[ticker].tplimitPrice = 0.0m;
                        }
                        orders[ticker].slOrder = null;
                        orders[ticker].limitOrder = null;
                        noOrderBarCount = 0;
                    }
                }
            }
        }
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            foreach (var ticker in tickers)
            {
                bool replace = orders[ticker].OnOrderEvent(orderEvent);
            }
        }
    }
}
