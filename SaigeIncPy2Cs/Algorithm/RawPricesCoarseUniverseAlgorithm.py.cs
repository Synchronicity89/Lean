
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using OrderStatus = QuantConnect.Orders.OrderStatus;

using ConstantFeeModel = QuantConnect.Orders.Fees.ConstantFeeModel;

using System.Collections.Generic;

using System.Linq;

public static class RawPricesCoarseUniverseAlgorithm {
    
    static RawPricesCoarseUniverseAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class RawPricesCoarseUniverseAlgorithm
        : QCAlgorithm {
        
        public int @__numberOfSymbols;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // what resolution should the data *added* to the universe be?
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2014, 1, 1);
            this.SetEndDate(2015, 1, 1);
            this.SetCash(50000);
            // Set the security initializer with the characteristics defined in CustomSecurityInitializer
            this.SetSecurityInitializer(this.CustomSecurityInitializer);
            // this add universe method accepts a single parameter that is a function that
            // accepts an IEnumerable<CoarseFundamental> and returns IEnumerable<Symbol>
            this.AddUniverse(this.CoarseSelectionFunction);
            this.@__numberOfSymbols = 5;
        }
        
        // Initialize the security with raw prices and zero fees 
        //         Args:
        //             security: Security which characteristics we want to change
        public virtual object CustomSecurityInitializer(object security) {
            security.SetDataNormalizationMode(DataNormalizationMode.Raw);
            security.SetFeeModel(ConstantFeeModel(0));
        }
        
        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        public virtual object CoarseSelectionFunction(object coarse) {
            // sort descending by daily dollar volume
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume).ToList();
            // return the symbol objects of the top entries from our sorted collection
            return (from x in sortedByDollarVolume[@::self.__numberOfSymbols]
                select x.Symbol).ToList();
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
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Log("OnOrderEvent({self.UtcTime}):: {orderEvent}");
            }
        }
    }
}
