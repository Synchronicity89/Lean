
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using OrderStatus = QuantConnect.Orders.OrderStatus;

using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;

using System.Collections.Generic;

public static class RawPricesUniverseRegressionAlgorithm {
    
    static RawPricesUniverseRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class RawPricesUniverseRegressionAlgorithm
        : QCAlgorithm {
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // what resolution should the data *added* to the universe be?
            this.UniverseSettings.Resolution = Resolution.Daily;
            // Use raw prices
            this.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            this.SetStartDate(2014, 3, 24);
            this.SetEndDate(2014, 4, 7);
            this.SetCash(50000);
            // Set the security initializer with zero fees
            this.SetSecurityInitializer(x => x.SetFeeModel(ConstantFeeModel(0)));
            this.AddUniverse("MyUniverse", Resolution.Daily, this.SelectionFunction);
        }
        
        public virtual object SelectionFunction(object dateTime) {
            if (dateTime.day % 2 == 0) {
                return new List<string> {
                    "SPY",
                    "IWM",
                    "QQQ"
                };
            } else {
                return new List<string> {
                    "AIG",
                    "BAC",
                    "IBM"
                };
            }
        }
        
        // this event fires whenever we have changes to our universe
        public virtual object OnSecuritiesChanged(object changes) {
            // liquidate removed securities
            foreach (var security in changes.RemovedSecurities) {
                if (security.Invested) {
                    this.Liquidate(security.Symbol);
                }
            }
            // we want 20% allocation in each security in our universe
            foreach (var security in changes.AddedSecurities) {
                this.SetHoldings(security.Symbol, 0.2);
            }
        }
    }
}
