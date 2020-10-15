
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

public static class AddRemoveSecurityRegressionAlgorithm {
    
    static AddRemoveSecurityRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class AddRemoveSecurityRegressionAlgorithm
        : QCAlgorithm {
        
        public object _lastAction;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY");
            this._lastAction = null;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (this._lastAction != null && this._lastAction.date() == this.Time.date()) {
                return;
            }
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 0.5);
                this._lastAction = this.Time;
            }
            if (this.Time.weekday() == 1) {
                this.AddEquity("AIG");
                this.AddEquity("BAC");
                this._lastAction = this.Time;
            }
            if (this.Time.weekday() == 2) {
                this.SetHoldings("AIG", 0.25);
                this.SetHoldings("BAC", 0.25);
                this._lastAction = this.Time;
            }
            if (this.Time.weekday() == 3) {
                this.RemoveSecurity("AIG");
                this.RemoveSecurity("BAC");
                this._lastAction = this.Time;
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Submitted) {
                this.Debug("{0}: Submitted: {1}".format(this.Time, this.Transactions.GetOrderById(orderEvent.OrderId)));
            }
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Debug("{0}: Filled: {1}".format(this.Time, this.Transactions.GetOrderById(orderEvent.OrderId)));
            }
        }
    }
}
