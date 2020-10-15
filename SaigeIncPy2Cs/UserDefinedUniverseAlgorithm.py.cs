
using AddReference = clr.AddReference;

using List = System.Collections.Generic.List;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using System.Collections.Generic;

public static class UserDefinedUniverseAlgorithm {
    
    static UserDefinedUniverseAlgorithm() {
        AddReference("System.Core");
        AddReference("System.Collections");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class UserDefinedUniverseAlgorithm
        : QCAlgorithm {
        
        public List<string> symbols;
        
        public virtual object Initialize() {
            this.SetCash(100000);
            this.SetStartDate(2015, 1, 1);
            this.SetEndDate(2015, 12, 1);
            this.symbols = new List<string> {
                "SPY",
                "GOOG",
                "IBM",
                "AAPL",
                "MSFT",
                "CSCO",
                "ADBE",
                "WMT"
            };
            this.UniverseSettings.Resolution = Resolution.Hour;
            this.AddUniverse("my_universe_name", Resolution.Hour, this.selection);
        }
        
        public virtual object selection(object time) {
            var index = time.hour % this.symbols.Count;
            return this.symbols[index];
        }
        
        public virtual object OnData(object slice) {
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            foreach (var removed in changes.RemovedSecurities) {
                if (removed.Invested) {
                    this.Liquidate(removed.Symbol);
                }
            }
            foreach (var added in changes.AddedSecurities) {
                this.SetHoldings(added.Symbol, 1 / float(changes.AddedSecurities.Count));
            }
        }
    }
}
