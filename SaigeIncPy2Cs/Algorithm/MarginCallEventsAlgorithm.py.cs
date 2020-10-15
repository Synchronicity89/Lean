
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using np = numpy;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System;

public static class MarginCallEventsAlgorithm {
    
    static MarginCallEventsAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    // 
    //     This algorithm showcases two margin related event handlers.
    //     OnMarginCallWarning: Fired when a portfolio's remaining margin dips below 5% of the total portfolio value
    //     OnMarginCall: Fired immediately before margin call orders are execued, this gives the algorithm a change to regain margin on its own through liquidation
    //     
    public class MarginCallEventsAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetCash(100000);
            this.SetStartDate(2013, 10, 1);
            this.SetEndDate(2013, 12, 11);
            this.AddEquity("SPY", Resolution.Second);
            // cranking up the leverage increases the odds of a margin call
            // when the security falls in value
            this.Securities["SPY"].SetLeverage(100);
        }
        
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 100);
            }
        }
        
        public virtual object OnMarginCall(object requests) {
            // Margin call event handler. This method is called right before the margin call orders are placed in the market.
            // <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
            // this code gets called BEFORE the orders are placed, so we can try to liquidate some of our positions
            // before we get the margin call orders executed. We could also modify these orders by changing their quantities
            foreach (var order in requests) {
                // liquidate an extra 10% each time we get a margin call to give us more padding
                var newQuantity = Convert.ToInt32(np.sign(order.Quantity) * order.Quantity * 1.1);
                requests.remove(order);
                requests.append(SubmitOrderRequest(order.OrderType, order.SecurityType, order.Symbol, newQuantity, order.StopPrice, order.LimitPrice, this.Time, "OnMarginCall"));
            }
            return requests;
        }
        
        public virtual object OnMarginCallWarning() {
            // Margin call warning event handler.
            // This method is called when Portfolio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
            // a chance to prevent a margin call from occurring
            var spyHoldings = this.Securities["SPY"].Holdings.Quantity;
            var shares = Convert.ToInt32(-spyHoldings * 0.005);
            this.Error("{0} - OnMarginCallWarning(): Liquidating {1} shares of SPY to avoid margin call.".format(this.Time, shares));
            this.MarketOrder("SPY", shares);
        }
    }
}
