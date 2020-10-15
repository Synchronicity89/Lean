
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using System.Collections.Generic;

using System.Linq;

public static class UniverseSelectionRegressionAlgorithm {
    
    static UniverseSelectionRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class UniverseSelectionRegressionAlgorithm
        : QCAlgorithm {
        
        public None changes;
        
        public List<object> delistedSymbols;
        
        public virtual object Initialize() {
            this.SetStartDate(2014, 3, 22);
            this.SetEndDate(2014, 4, 7);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            // security that exists with no mappings
            this.AddEquity("SPY", Resolution.Daily);
            // security that doesn't exist until half way in backtest (comes in as GOOCV)
            this.AddEquity("GOOG", Resolution.Daily);
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.AddUniverse(this.CoarseSelectionFunction);
            this.delistedSymbols = new List<object>();
            this.changes = null;
        }
        
        public virtual object CoarseSelectionFunction(object coarse) {
            return (from c in coarse
                where c.Symbol.Value == "GOOG" || c.Symbol.Value == "GOOCV" || c.Symbol.Value == "GOOAV" || c.Symbol.Value == "GOOGL"
                select c.Symbol).ToList();
        }
        
        public virtual object OnData(object data) {
            if (this.Transactions.OrdersCount == 0) {
                this.MarketOrder("SPY", 100);
            }
            foreach (var kvp in data.Delistings) {
                this.delistedSymbols.append(kvp.Key);
            }
            if (this.changes == null) {
                return;
            }
            if (!all(from x in this.changes.AddedSecurities
                select data.Bars.ContainsKey(x.Symbol))) {
                return;
            }
            foreach (var security in this.changes.AddedSecurities) {
                this.Log("{0}: Added Security: {1}".format(this.Time, security.Symbol));
                this.MarketOnOpenOrder(security.Symbol, 100);
            }
            foreach (var security in this.changes.RemovedSecurities) {
                this.Log("{0}: Removed Security: {1}".format(this.Time, security.Symbol));
                if (!this.delistedSymbols.Contains(security.Symbol)) {
                    this.Log("Not in delisted: {0}:".format(security.Symbol));
                    this.MarketOnOpenOrder(security.Symbol, -100);
                }
            }
            this.changes = null;
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            this.changes = changes;
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Submitted) {
                this.Log("{0}: Submitted: {1}".format(this.Time, this.Transactions.GetOrderById(orderEvent.OrderId)));
            }
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Log("{0}: Filled: {1}".format(this.Time, this.Transactions.GetOrderById(orderEvent.OrderId)));
            }
        }
    }
}
