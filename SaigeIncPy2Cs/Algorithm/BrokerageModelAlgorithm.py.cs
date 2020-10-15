
using AddReference = clr.AddReference;

public static class BrokerageModelAlgorithm {
    
    static BrokerageModelAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BrokerageModelAlgorithm
        : QCAlgorithm {
        
        public double last;
        
        public virtual object Initialize() {
            this.SetCash(100000);
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.AddEquity("SPY", Resolution.Second);
            // there's two ways to set your brokerage model. The easiest would be to call
            // SetBrokerageModel( BrokerageName ); // BrokerageName is an enum
            // SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            // SetBrokerageModel(BrokerageName.Default);
            // the other way is to call SetBrokerageModel( IBrokerageModel ) with your
            // own custom model. I've defined a simple extension to the default brokerage
            // model to take into account a requirement to maintain 500 cash in the account at all times
            this.SetBrokerageModel(new MinimumAccountBalanceBrokerageModel(this, 500.0));
            this.last = 1;
        }
        
        public virtual object OnData(object slice) {
            // Simple buy and hold template
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", this.last);
                if (this.Portfolio["SPY"].Quantity == 0) {
                    // each time we fail to purchase we'll decrease our set holdings percentage
                    this.Debug(this.Time.ToString() + " - Failed to purchase stock");
                    this.last *= 0.95;
                } else {
                    this.Debug("{} - Purchased Stock @ SetHoldings( {} )".format(this.Time, this.last));
                }
            }
        }
    }
    
    // Custom brokerage model that requires clients to maintain a minimum cash balance
    public class MinimumAccountBalanceBrokerageModel
        : DefaultBrokerageModel {
        
        public object algorithm;
        
        public object minimumAccountBalance;
        
        public MinimumAccountBalanceBrokerageModel(object algorithm, object minimumAccountBalance) {
            this.algorithm = algorithm;
            this.minimumAccountBalance = minimumAccountBalance;
        }
        
        // Prevent orders which would bring the account below a minimum cash balance
        public virtual object CanSubmitOrder(object security, object order, object message) {
            message = null;
            // we want to model brokerage requirement of minimumAccountBalance cash value in account
            var orderCost = order.GetValue(security);
            var cash = this.algorithm.Portfolio.Cash;
            var cashAfterOrder = cash - orderCost;
            if (cashAfterOrder < this.minimumAccountBalance) {
                // return a message describing why we're not allowing this order
                message = BrokerageMessageEvent(BrokerageMessageType.Warning, "InsufficientRemainingCapital", "Account must maintain a minimum of ${0} USD at all times. Order ID: {1}".format(this.minimumAccountBalance, order.Id));
                this.algorithm.Error(message.ToString());
                return false;
            }
            return true;
        }
    }
}
