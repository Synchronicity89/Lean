
using AddReference = clr.AddReference;

using copysign = math.copysign;

using datetime = datetime.datetime;

using System.Collections.Generic;

using System.Linq;

public static class UpdateOrderRegressionAlgorithm {
    
    static UpdateOrderRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("System.Collections");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class UpdateOrderRegressionAlgorithm
        : QCAlgorithm {
        
        public int delta_quantity;
        
        public object last_month;
        
        public double limit_percentage;
        
        public double limit_percentage_delta;
        
        public object order_types_queue;
        
        public int quantity;
        
        public object security;
        
        public double stop_percentage;
        
        public double stop_percentage_delta;
        
        public List<object> tickets;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.security = this.AddEquity("SPY", Resolution.Daily);
            this.last_month = -1;
            this.quantity = 100;
            this.delta_quantity = 10;
            this.stop_percentage = 0.025;
            this.stop_percentage_delta = 0.005;
            this.limit_percentage = 0.025;
            this.limit_percentage_delta = 0.005;
            var OrderTypeEnum = new List<object> {
                OrderType.Market,
                OrderType.Limit,
                OrderType.StopMarket,
                OrderType.StopLimit,
                OrderType.MarketOnOpen,
                OrderType.MarketOnClose
            };
            this.order_types_queue = CircularQueue[OrderType](OrderTypeEnum);
            this.order_types_queue.CircleCompleted += this.onCircleCompleted;
            this.tickets = new List<object>();
        }
        
        // Flip our signs when we've gone through all the order types
        public virtual object onCircleCompleted(object sender, object @event) {
            this.quantity *= -1;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            object updateOrderFields;
            object ticket;
            if (!data.ContainsKey("SPY")) {
                return;
            }
            if (this.Time.month != this.last_month) {
                // we'll submit the next type of order from the queue
                var orderType = this.order_types_queue.Dequeue();
                //Log("")
                this.Log("\r\n--------------MONTH: {0}:: {1}\r\n".format(this.Time.strftime("%B"), orderType));
                //Log("")
                this.last_month = this.Time.month;
                this.Log("ORDER TYPE:: {0}".format(orderType));
                var isLong = this.quantity > 0;
                var stopPrice = isLong ? (1 + this.stop_percentage) * data["SPY"].High : (1 - this.stop_percentage) * data["SPY"].Low;
                var limitPrice = isLong ? (1 - this.limit_percentage) * stopPrice : (1 + this.limit_percentage) * stopPrice;
                if (orderType == OrderType.Limit) {
                    limitPrice = !isLong ? (1 + this.limit_percentage) * data["SPY"].High : (1 - this.limit_percentage) * data["SPY"].Low;
                }
                var request = SubmitOrderRequest(orderType, this.security.Symbol.SecurityType, "SPY", this.quantity, stopPrice, limitPrice, this.UtcTime, orderType.ToString());
                ticket = this.Transactions.AddOrder(request);
                this.tickets.append(ticket);
            } else if (this.tickets.Count > 0) {
                ticket = this.tickets[-1];
                if (this.Time.day > 8 && this.Time.day < 14) {
                    if (ticket.UpdateRequests.Count == 0 && ticket.Status != OrderStatus.Filled) {
                        this.Log("TICKET:: {0}".format(ticket));
                        updateOrderFields = UpdateOrderFields();
                        updateOrderFields.Quantity = ticket.Quantity + copysign(this.delta_quantity, this.quantity);
                        updateOrderFields.Tag = "Change quantity: {0}".format(this.Time);
                        ticket.Update(updateOrderFields);
                    }
                } else if (this.Time.day > 13 && this.Time.day < 20) {
                    if (ticket.UpdateRequests.Count == 1 && ticket.Status != OrderStatus.Filled) {
                        this.Log("TICKET:: {0}".format(ticket));
                        updateOrderFields = UpdateOrderFields();
                        updateOrderFields.LimitPrice = this.security.Price * (1 - copysign(this.limit_percentage_delta, ticket.Quantity));
                        updateOrderFields.StopPrice = this.security.Price * (1 + copysign(this.stop_percentage_delta, ticket.Quantity));
                        updateOrderFields.Tag = "Change prices: {0}".format(this.Time);
                        ticket.Update(updateOrderFields);
                    }
                } else if (ticket.UpdateRequests.Count == 2 && ticket.Status != OrderStatus.Filled) {
                    this.Log("TICKET:: {0}".format(ticket));
                    ticket.Cancel("{0} and is still open!".format(this.Time));
                    this.Log("CANCELLED:: {0}".format(ticket.CancelRequest));
                }
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            var order = this.Transactions.GetOrderById(orderEvent.OrderId);
            var ticket = this.Transactions.GetOrderTicket(orderEvent.OrderId);
            //order cancelations update CanceledTime
            if (order.Status == OrderStatus.Canceled && order.CanceledTime != orderEvent.UtcTime) {
                throw new ValueError("Expected canceled order CanceledTime to equal canceled order event time.");
            }
            //fills update LastFillTime
            if ((order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled) && order.LastFillTime != orderEvent.UtcTime) {
                throw new ValueError("Expected filled order LastFillTime to equal fill order event time.");
            }
            // check the ticket to see if the update was successfully processed
            if ((from ur in ticket.UpdateRequests
                where ur.Response != null && ur.Response.IsSuccess
                select ur).ToList().Count > 0 && order.CreatedTime != this.UtcTime && order.LastUpdateTime == null) {
                throw new ValueError("Expected updated order LastUpdateTime to equal submitted update order event time");
            }
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Log("FILLED:: {0} FILL PRICE:: {1}".format(this.Transactions.GetOrderById(orderEvent.OrderId), orderEvent.FillPrice));
            } else {
                this.Log(orderEvent.ToString());
                this.Log("TICKET:: {0}".format(ticket));
            }
        }
    }
}
