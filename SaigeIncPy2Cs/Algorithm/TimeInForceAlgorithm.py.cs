
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using System.Collections.Generic;

public static class TimeInForceAlgorithm {
    
    static TimeInForceAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class TimeInForceAlgorithm
        : QCAlgorithm {
        
        public object dayOrderTicket1;
        
        public object dayOrderTicket2;
        
        public Dictionary<object, object> expectedOrderStatuses;
        
        public object gtcOrderTicket1;
        
        public object gtcOrderTicket2;
        
        public object gtdOrderTicket1;
        
        public object gtdOrderTicket2;
        
        public object symbol;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // The default time in force setting for all orders is GoodTilCancelled (GTC),
            // uncomment this line to set a different time in force.
            // We currently only support GTC and DAY.
            // self.DefaultOrderProperties.TimeInForce = TimeInForce.Day
            this.symbol = this.AddEquity("SPY", Resolution.Minute).Symbol;
            this.gtcOrderTicket1 = null;
            this.gtcOrderTicket2 = null;
            this.dayOrderTicket1 = null;
            this.dayOrderTicket2 = null;
            this.gtdOrderTicket1 = null;
            this.gtdOrderTicket2 = null;
            this.expectedOrderStatuses = new Dictionary<object, object> {
            };
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // Arguments:
        //    data: Slice object keyed by symbol containing the stock data
        public virtual object OnData(object data) {
            if (this.gtcOrderTicket1 == null) {
                // These GTC orders will never expire and will not be canceled automatically.
                this.DefaultOrderProperties.TimeInForce = TimeInForce.GoodTilCanceled;
                // this order will not be filled before the end of the backtest
                this.gtcOrderTicket1 = this.LimitOrder(this.symbol, 10, 100);
                this.expectedOrderStatuses[this.gtcOrderTicket1.OrderId] = OrderStatus.Submitted;
                // this order will be filled before the end of the backtest
                this.gtcOrderTicket2 = this.LimitOrder(this.symbol, 10, 160);
                this.expectedOrderStatuses[this.gtcOrderTicket2.OrderId] = OrderStatus.Filled;
            }
            if (this.dayOrderTicket1 == null) {
                // These DAY orders will expire at market close,
                // if not filled by then they will be canceled automatically.
                this.DefaultOrderProperties.TimeInForce = TimeInForce.Day;
                // this order will not be filled before market close and will be canceled
                this.dayOrderTicket1 = this.LimitOrder(this.symbol, 10, 150);
                this.expectedOrderStatuses[this.dayOrderTicket1.OrderId] = OrderStatus.Canceled;
                // this order will be filled before market close
                this.dayOrderTicket2 = this.LimitOrder(this.symbol, 10, 180);
                this.expectedOrderStatuses[this.dayOrderTicket2.OrderId] = OrderStatus.Filled;
            }
            if (this.gtdOrderTicket1 == null) {
                // These GTD orders will expire on October 10th at market close,
                // if not filled by then they will be canceled automatically.
                this.DefaultOrderProperties.TimeInForce = TimeInForce.GoodTilDate(new datetime(2013, 10, 10));
                // this order will not be filled before expiry and will be canceled
                this.gtdOrderTicket1 = this.LimitOrder(this.symbol, 10, 100);
                this.expectedOrderStatuses[this.gtdOrderTicket1.OrderId] = OrderStatus.Canceled;
                // this order will be filled before expiry
                this.gtdOrderTicket2 = this.LimitOrder(this.symbol, 10, 160);
                this.expectedOrderStatuses[this.gtdOrderTicket2.OrderId] = OrderStatus.Filled;
            }
        }
        
        // Order event handler. This handler will be called for all order events, including submissions, fills, cancellations.
        // This method can be called asynchronously, ensure you use proper locks on thread-unsafe objects
        public virtual object OnOrderEvent(object orderEvent) {
            this.Debug("{self.Time} {orderEvent}");
        }
        
        // End of algorithm run event handler. This method is called at the end of a backtest or live trading operation.
        public virtual object OnEndOfAlgorithm() {
            foreach (var _tup_1 in this.expectedOrderStatuses.items()) {
                var orderId = _tup_1.Item1;
                var expectedStatus = _tup_1.Item2;
                var order = this.Transactions.GetOrderById(orderId);
                if (order.Status != expectedStatus) {
                    throw new Exception("Invalid status for order {orderId} - Expected: {expectedStatus}, actual: {order.Status}");
                }
            }
        }
    }
}
