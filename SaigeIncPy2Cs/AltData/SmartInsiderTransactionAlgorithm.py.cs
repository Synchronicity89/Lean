namespace AltData {
    
    using AddReference = clr.AddReference;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class SmartInsiderTransactionAlgorithm {
        
        static SmartInsiderTransactionAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class SmartInsiderTransactionAlgorithm
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2019, 3, 1);
                this.SetEndDate(2019, 7, 4);
                this.SetCash(1000000);
                this.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(this.CoarseUniverse));
                // Request underlying equity data.
                var ibm = this.AddEquity("IBM", Resolution.Minute).Symbol;
                // Add Smart Insider stock buyback transaction data for the underlying IBM asset
                var si = this.AddData(SmartInsiderTransaction, ibm).Symbol;
                // Request 60 days of history with the SmartInsiderTransaction IBM Custom Data Symbol
                var history = this.History(SmartInsiderTransaction, si, 60, Resolution.Daily);
                // Count the number of items we get from our history request
                this.Debug("We got {len(history)} items from our history request");
            }
            
            public virtual object CoarseUniverse(object coarse) {
                var symbols = (from i in coarse
                    where i.HasFundamentalData && i.DollarVolume > 50000000
                    select i.Symbol).ToList()[::10];
                foreach (var symbol in symbols) {
                    this.AddData(SmartInsiderTransaction, symbol);
                }
                return symbols;
            }
            
            public virtual object OnData(object data) {
                // Get all SmartInsider data available
                var transactions = data.Get(SmartInsiderTransaction);
                // Loop over all the insider transactions
                foreach (var transaction in transactions.Values) {
                    if (transaction.VolumePercentage == null || transaction.EventType == null) {
                        continue;
                    }
                    // Using the SmartInsider transaction information, buy when company does a stock buyback
                    if (transaction.EventType == SmartInsiderEventType.Transaction && transaction.VolumePercentage > 5) {
                        this.SetHoldings(transaction.Symbol.Underlying, transaction.VolumePercentage / 100);
                    }
                }
            }
            
            public virtual object OnSecuritiesChanged(object changes) {
                foreach (var r in changes.RemovedSecurities) {
                    // If removed from the universe, liquidate and remove the custom data from the algorithm
                    this.Liquidate(r.Symbol);
                    this.RemoveSecurity(Symbol.CreateBase(SmartInsiderTransaction, r.Symbol, Market.USA));
                }
            }
        }
    }
}
