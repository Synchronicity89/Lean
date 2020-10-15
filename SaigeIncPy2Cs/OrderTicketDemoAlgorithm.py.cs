
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class OrderTicketDemoAlgorithm {
    
    static OrderTicketDemoAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // In this algorithm we submit/update/cancel each order type
    public class OrderTicketDemoAlgorithm
        : QCAlgorithm {
        
        public List<object> @__openLimitOrders;
        
        public List<object> @__openMarketOnCloseOrders;
        
        public List<object> @__openMarketOnOpenOrders;
        
        public List<object> @__openStopLimitOrders;
        
        public List<object> @__openStopMarketOrders;
        
        public object spy;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            var equity = this.AddEquity("SPY");
            this.spy = equity.Symbol;
            this.@__openMarketOnOpenOrders = new List<object>();
            this.@__openMarketOnCloseOrders = new List<object>();
            this.@__openLimitOrders = new List<object>();
            this.@__openStopMarketOrders = new List<object>();
            this.@__openStopLimitOrders = new List<object>();
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            // MARKET ORDERS
            this.MarketOrders();
            // LIMIT ORDERS
            this.LimitOrders();
            // STOP MARKET ORDERS
            this.StopMarketOrders();
            //# STOP LIMIT ORDERS
            this.StopLimitOrders();
            //# MARKET ON OPEN ORDERS
            this.MarketOnOpenOrders();
            //# MARKET ON CLOSE ORDERS
            this.MarketOnCloseOrders();
        }
        
        //  MarketOrders are the only orders that are processed synchronously by default, so
        //         they'll fill by the next line of code. This behavior equally applies to live mode.
        //         You can opt out of this behavior by specifying the 'asynchronous' parameter as True.
        public virtual object MarketOrders() {
            if (this.TimeIs(7, 9, 31)) {
                this.Log("Submitting MarketOrder");
                // submit a market order to buy 10 shares, this function returns an OrderTicket object
                // we submit the order with asynchronous = False, so it block until it is filled
                var newTicket = this.MarketOrder(this.spy, 10, asynchronous: false);
                if (newTicket.Status != OrderStatus.Filled) {
                    this.Log("Synchronous market order was not filled synchronously!");
                    this.Quit();
                }
                // we can also submit the ticket asynchronously. In a backtest, we'll still perform the fill
                // before the next time events for your algorithm. here we'll submit the order asynchronously
                // and try to cancel it, sometimes it will, sometimes it will be filled first.
                newTicket = this.MarketOrder(this.spy, 10, asynchronous: true);
                var response = newTicket.Cancel("Attempt to cancel async order");
                if (response.IsSuccess) {
                    this.Log("Successfully canceled async market order: {0}".format(newTicket.OrderId));
                } else {
                    this.Log("Unable to cancel async market order: {0}".format(response.ErrorCode));
                }
            }
        }
        
        // LimitOrders are always processed asynchronously. Limit orders are used to
        //         set 'good' entry points for an order. For example, you may wish to go
        //         long a stock, but want a good price, so can place a LimitOrder to buy with
        //         a limit price below the current market price. Likewise the opposite is True
        //         when selling, you can place a LimitOrder to sell with a limit price above the
        //         current market price to get a better sale price.
        //         You can submit requests to update or cancel the LimitOrder at any time.
        //         The 'LimitPrice' for an order can be retrieved from the ticket using the
        //         OrderTicket.Get(OrderField) method, for example:
        //         Code:
        //             currentLimitPrice = orderTicket.Get(OrderField.LimitPrice)
        public virtual object LimitOrders() {
            if (this.TimeIs(7, 12, 0)) {
                this.Log("Submitting LimitOrder");
                // submit a limit order to buy 10 shares at .1% below the bar's close
                var close = this.Securities[this.spy.Value].Close;
                var newTicket = this.LimitOrder(this.spy, 10, close * 0.999);
                this.@__openLimitOrders.append(newTicket);
                // submit another limit order to sell 10 shares at .1% above the bar's close
                newTicket = this.LimitOrder(this.spy, -10, close * 1.001);
                this.@__openLimitOrders.append(newTicket);
            }
            // when we submitted new limit orders we placed them into this list,
            // so while there's two entries they're still open and need processing
            if (this.@__openLimitOrders.Count == 2) {
                var openOrders = this.@__openLimitOrders;
                // check if either is filled and cancel the other
                var longOrder = openOrders[0];
                var shortOrder = openOrders[1];
                if (this.CheckPairOrdersForFills(longOrder, shortOrder)) {
                    this.@__openLimitOrders = new List<object>();
                    return;
                }
                // if niether order has filled, bring in the limits by a penny
                var newLongLimit = longOrder.Get(OrderField.LimitPrice) + 0.01;
                var newShortLimit = shortOrder.Get(OrderField.LimitPrice) - 0.01;
                this.Log("Updating limits - Long: {0:.2f} Short: {1:.2f}".format(newLongLimit, newShortLimit));
                var updateOrderFields = UpdateOrderFields();
                updateOrderFields.LimitPrice = newLongLimit;
                updateOrderFields.Tag = "Update #{0}".format(longOrder.UpdateRequests.Count + 1);
                longOrder.Update(updateOrderFields);
                updateOrderFields = UpdateOrderFields();
                updateOrderFields.LimitPrice = newShortLimit;
                updateOrderFields.Tag = "Update #{0}".format(shortOrder.UpdateRequests.Count + 1);
                shortOrder.Update(updateOrderFields);
            }
        }
        
        // StopMarketOrders work in the opposite way that limit orders do.
        //         When placing a long trade, the stop price must be above current
        //         market price. In this way it's a 'stop loss' for a short trade.
        //         When placing a short trade, the stop price must be below current
        //         market price. In this way it's a 'stop loss' for a long trade.
        //         You can submit requests to update or cancel the StopMarketOrder at any time.
        //         The 'StopPrice' for an order can be retrieved from the ticket using the
        //         OrderTicket.Get(OrderField) method, for example:
        //         Code:
        //             currentStopPrice = orderTicket.Get(OrderField.StopPrice)
        public virtual object StopMarketOrders() {
            if (this.TimeIs(7, 12 + 4, 0)) {
                this.Log("Submitting StopMarketOrder");
                // a long stop is triggered when the price rises above the value
                // so we'll set a long stop .25% above the current bar's close
                var close = this.Securities[this.spy.Value].Close;
                var newTicket = this.StopMarketOrder(this.spy, 10, close * 1.0025);
                this.@__openStopMarketOrders.append(newTicket);
                // a short stop is triggered when the price falls below the value
                // so we'll set a short stop .25% below the current bar's close
                newTicket = this.StopMarketOrder(this.spy, -10, close * 0.9975);
                this.@__openStopMarketOrders.append(newTicket);
            }
            // when we submitted new stop market orders we placed them into this list,
            // so while there's two entries they're still open and need processing
            if (this.@__openStopMarketOrders.Count == 2) {
                // check if either is filled and cancel the other
                var longOrder = this.@__openStopMarketOrders[0];
                var shortOrder = this.@__openStopMarketOrders[1];
                if (this.CheckPairOrdersForFills(longOrder, shortOrder)) {
                    this.@__openStopMarketOrders = new List<object>();
                    return;
                }
                // if neither order has filled, bring in the stops by a penny
                var newLongStop = longOrder.Get(OrderField.StopPrice) - 0.01;
                var newShortStop = shortOrder.Get(OrderField.StopPrice) + 0.01;
                this.Log("Updating stops - Long: {0:.2f} Short: {1:.2f}".format(newLongStop, newShortStop));
                var updateOrderFields = UpdateOrderFields();
                updateOrderFields.StopPrice = newLongStop;
                updateOrderFields.Tag = "Update #{0}".format(longOrder.UpdateRequests.Count + 1);
                longOrder.Update(updateOrderFields);
                updateOrderFields = UpdateOrderFields();
                updateOrderFields.StopPrice = newShortStop;
                updateOrderFields.Tag = "Update #{0}".format(shortOrder.UpdateRequests.Count + 1);
                shortOrder.Update(updateOrderFields);
                this.Log("Updated price - Long: {0} Short: {1}".format(longOrder.Get(OrderField.StopPrice), shortOrder.Get(OrderField.StopPrice)));
            }
        }
        
        // StopLimitOrders work as a combined stop and limit order. First, the
        //         price must pass the stop price in the same way a StopMarketOrder works,
        //         but then we're also gauranteed a fill price at least as good as the
        //         limit price. This order type can be beneficial in gap down scenarios
        //         where a StopMarketOrder would have triggered and given the not as beneficial
        //         gapped down price, whereas the StopLimitOrder could protect you from
        //         getting the gapped down price through prudent placement of the limit price.
        //         You can submit requests to update or cancel the StopLimitOrder at any time.
        //         The 'StopPrice' or 'LimitPrice' for an order can be retrieved from the ticket
        //         using the OrderTicket.Get(OrderField) method, for example:
        //         Code:
        //             currentStopPrice = orderTicket.Get(OrderField.StopPrice)
        //             currentLimitPrice = orderTicket.Get(OrderField.LimitPrice)
        public virtual object StopLimitOrders() {
            if (this.TimeIs(8, 12, 1)) {
                this.Log("Submitting StopLimitOrder");
                // a long stop is triggered when the price rises above the
                // value so we'll set a long stop .25% above the current bar's
                // close now we'll also be setting a limit, this means we are
                // gauranteed to get at least the limit price for our fills,
                // so make the limit price a little higher than the stop price
                var close = this.Securities[this.spy.Value].Close;
                var newTicket = this.StopLimitOrder(this.spy, 10, close * 1.001, close * 1.0025);
                this.@__openStopLimitOrders.append(newTicket);
                // a short stop is triggered when the price falls below the
                // value so we'll set a short stop .25% below the current bar's
                // close now we'll also be setting a limit, this means we are
                // gauranteed to get at least the limit price for our fills,
                // so make the limit price a little softer than the stop price
                newTicket = this.StopLimitOrder(this.spy, -10, close * 0.999, close * 0.9975);
                this.@__openStopLimitOrders.append(newTicket);
            }
            // when we submitted new stop limit orders we placed them into this list,
            // so while there's two entries they're still open and need processing
            if (this.@__openStopLimitOrders.Count == 2) {
                var longOrder = this.@__openStopLimitOrders[0];
                var shortOrder = this.@__openStopLimitOrders[1];
                if (this.CheckPairOrdersForFills(longOrder, shortOrder)) {
                    this.@__openStopLimitOrders = new List<object>();
                    return;
                }
                // if neither order has filled, bring in the stops/limits in by a penny
                var newLongStop = longOrder.Get(OrderField.StopPrice) - 0.01;
                var newLongLimit = longOrder.Get(OrderField.LimitPrice) + 0.01;
                var newShortStop = shortOrder.Get(OrderField.StopPrice) + 0.01;
                var newShortLimit = shortOrder.Get(OrderField.LimitPrice) - 0.01;
                this.Log("Updating stops  - Long: {0:.2f} Short: {1:.2f}".format(newLongStop, newShortStop));
                this.Log("Updating limits - Long: {0:.2f}  Short: {1:.2f}".format(newLongLimit, newShortLimit));
                var updateOrderFields = UpdateOrderFields();
                updateOrderFields.StopPrice = newLongStop;
                updateOrderFields.LimitPrice = newLongLimit;
                updateOrderFields.Tag = "Update #{0}".format(longOrder.UpdateRequests.Count + 1);
                longOrder.Update(updateOrderFields);
                updateOrderFields = UpdateOrderFields();
                updateOrderFields.StopPrice = newShortStop;
                updateOrderFields.LimitPrice = newShortLimit;
                updateOrderFields.Tag = "Update #{0}".format(shortOrder.UpdateRequests.Count + 1);
                shortOrder.Update(updateOrderFields);
            }
        }
        
        // MarketOnCloseOrders are always executed at the next market's closing price.
        //         The only properties that can be updated are the quantity and order tag properties.
        public virtual object MarketOnCloseOrders() {
            if (this.TimeIs(9, 12, 0)) {
                this.Log("Submitting MarketOnCloseOrder");
                // open a new position or triple our existing position
                var qty = this.Portfolio[this.spy.Value].Quantity;
                qty = qty == 0 ? 100 : 2 * qty;
                var newTicket = this.MarketOnCloseOrder(this.spy, qty);
                this.@__openMarketOnCloseOrders.append(newTicket);
            }
            if (this.@__openMarketOnCloseOrders.Count == 1 && this.Time.minute == 59) {
                var ticket = this.@__openMarketOnCloseOrders[0];
                // check for fills
                if (ticket.Status == OrderStatus.Filled) {
                    this.@__openMarketOnCloseOrders = new List<object>();
                    return;
                }
                var quantity = ticket.Quantity + 1;
                this.Log("Updating quantity  - New Quantity: {0}".format(quantity));
                // we can update the quantity and tag
                var updateOrderFields = UpdateOrderFields();
                updateOrderFields.Quantity = quantity;
                updateOrderFields.Tag = "Update #{0}".format(ticket.UpdateRequests.Count + 1);
                ticket.Update(updateOrderFields);
            }
            if (this.TimeIs(this.EndDate.day, 12 + 3, 45)) {
                this.Log("Submitting MarketOnCloseOrder to liquidate end of algorithm");
                this.MarketOnCloseOrder(this.spy, -this.Portfolio[this.spy.Value].Quantity, "Liquidate end of algorithm");
            }
        }
        
        // MarketOnOpenOrders are always executed at the next
        //         market's opening price. The only properties that can
        //         be updated are the quantity and order tag properties.
        public virtual object MarketOnOpenOrders() {
            if (this.TimeIs(8, 12 + 2, 0)) {
                this.Log("Submitting MarketOnOpenOrder");
                // its EOD, let's submit a market on open order to short even more!
                var newTicket = this.MarketOnOpenOrder(this.spy, 50);
                this.@__openMarketOnOpenOrders.append(newTicket);
            }
            if (this.@__openMarketOnOpenOrders.Count == 1 && this.Time.minute == 59) {
                var ticket = this.@__openMarketOnOpenOrders[0];
                // check for fills
                if (ticket.Status == OrderStatus.Filled) {
                    this.@__openMarketOnOpenOrders = new List<object>();
                    return;
                }
                var quantity = ticket.Quantity + 1;
                this.Log("Updating quantity  - New Quantity: {0}".format(quantity));
                // we can update the quantity and tag
                var updateOrderFields = UpdateOrderFields();
                updateOrderFields.Quantity = quantity;
                updateOrderFields.Tag = "Update #{0}".format(ticket.UpdateRequests.Count + 1);
                ticket.Update(updateOrderFields);
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            var order = this.Transactions.GetOrderById(orderEvent.OrderId);
            this.Log("{0}: {1}: {2}".format(this.Time, order.Type, orderEvent));
        }
        
        public virtual object CheckPairOrdersForFills(object longOrder, object shortOrder) {
            if (longOrder.Status == OrderStatus.Filled) {
                this.Log("{0}: Cancelling short order, long order is filled.".format(shortOrder.OrderType));
                shortOrder.Cancel("Long filled.");
                return true;
            }
            if (shortOrder.Status == OrderStatus.Filled) {
                this.Log("{0}: Cancelling long order, short order is filled.".format(longOrder.OrderType));
                longOrder.Cancel("Short filled");
                return true;
            }
            return false;
        }
        
        public virtual object TimeIs(object day, object hour, object minute) {
            return this.Time.day == day && this.Time.hour == hour && this.Time.minute == minute;
        }
    }
}
