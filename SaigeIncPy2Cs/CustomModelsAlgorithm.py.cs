
using AddReference = clr.AddReference;

using np = numpy;

using random;

using System.Collections.Generic;

using System;

public static class CustomModelsAlgorithm {
    
    static CustomModelsAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Demonstration of using custom fee, slippage and fill models for modelling transactions in backtesting.
    //     QuantConnect allows you to model all orders as deeply and accurately as you need.
    public class CustomModelsAlgorithm
        : QCAlgorithm {
        
        public object security;
        
        public object spy;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 1);
            this.SetEndDate(2013, 10, 31);
            this.security = this.AddEquity("SPY", Resolution.Hour);
            this.spy = this.security.Symbol;
            // set our models
            this.security.SetFeeModel(new CustomFeeModel(this));
            this.security.SetFillModel(new CustomFillModel(this));
            this.security.SetSlippageModel(new CustomSlippageModel(this));
        }
        
        public virtual object OnData(object data) {
            object quantity;
            var open_orders = this.Transactions.GetOpenOrders(this.spy);
            if (open_orders.Count != 0) {
                return;
            }
            if (this.Time.day > 10 && this.security.Holdings.Quantity <= 0) {
                quantity = this.CalculateOrderQuantity(this.spy, 0.5);
                this.Log("MarketOrder: " + quantity.ToString());
                this.MarketOrder(this.spy, quantity, true);
            } else if (this.Time.day > 20 && this.security.Holdings.Quantity >= 0) {
                quantity = this.CalculateOrderQuantity(this.spy, -0.5);
                this.Log("MarketOrder: " + quantity.ToString());
                this.MarketOrder(this.spy, quantity, true);
            }
        }
    }
    
    public class CustomFillModel
        : ImmediateFillModel {
        
        public Dictionary<object, object> absoluteRemainingByOrderId;
        
        public object algorithm;
        
        public object random;
        
        public CustomFillModel(object algorithm) {
            this.algorithm = algorithm;
            this.absoluteRemainingByOrderId = new Dictionary<object, object> {
            };
            this.random = Random(387510346);
        }
        
        public virtual object MarketFill(object asset, object order) {
            var absoluteRemaining = order.AbsoluteQuantity;
            if (this.absoluteRemainingByOrderId.keys().Contains(order.Id)) {
                absoluteRemaining = this.absoluteRemainingByOrderId[order.Id];
            }
            var fill = super().MarketFill(asset, order);
            var absoluteFillQuantity = Convert.ToInt32(min(absoluteRemaining, this.random.Next(0, 2 * Convert.ToInt32(order.AbsoluteQuantity))));
            fill.FillQuantity = np.sign(order.Quantity) * absoluteFillQuantity;
            if (absoluteRemaining == absoluteFillQuantity) {
                fill.Status = OrderStatus.Filled;
                if (this.absoluteRemainingByOrderId.get(order.Id)) {
                    this.absoluteRemainingByOrderId.pop(order.Id);
                }
            } else {
                absoluteRemaining = absoluteRemaining - absoluteFillQuantity;
                this.absoluteRemainingByOrderId[order.Id] = absoluteRemaining;
                fill.Status = OrderStatus.PartiallyFilled;
            }
            this.algorithm.Log("CustomFillModel: " + fill.ToString());
            return fill;
        }
    }
    
    public class CustomFeeModel
        : FeeModel {
        
        public object algorithm;
        
        public CustomFeeModel(object algorithm) {
            this.algorithm = algorithm;
        }
        
        public virtual object GetOrderFee(object parameters) {
            // custom fee math
            var fee = max(1, parameters.Security.Price * parameters.Order.AbsoluteQuantity * 1E-05);
            this.algorithm.Log("CustomFeeModel: " + fee.ToString());
            return OrderFee(CashAmount(fee, "USD"));
        }
    }
    
    public class CustomSlippageModel {
        
        public object algorithm;
        
        public CustomSlippageModel(object algorithm) {
            this.algorithm = algorithm;
        }
        
        public virtual object GetSlippageApproximation(object asset, object order) {
            // custom slippage math
            var slippage = asset.Price * 0.0001 * np.log10(2 * float(order.AbsoluteQuantity));
            this.algorithm.Log("CustomSlippageModel: " + slippage.ToString());
            return slippage;
        }
    }
}
