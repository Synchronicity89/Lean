
using AddReference = clr.AddReference;

using System.Collections.Generic;

public static class CustomDataAddDataCoarseSelectionRegressionAlgorithm {
    
    static CustomDataAddDataCoarseSelectionRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomDataAddDataCoarseSelectionRegressionAlgorithm
        : QCAlgorithm {
        
        public List<object> customSymbols;
        
        public virtual object Initialize() {
            this.SetStartDate(2014, 3, 24);
            this.SetEndDate(2014, 4, 7);
            this.SetCash(100000);
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(this.CoarseSelector));
        }
        
        public virtual object CoarseSelector(object coarse) {
            var symbols = new List<object> {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                Symbol.Create("FB", SecurityType.Equity, Market.USA),
                Symbol.Create("GOOGL", SecurityType.Equity, Market.USA),
                Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                Symbol.Create("IBM", SecurityType.Equity, Market.USA)
            };
            this.customSymbols = new List<object>();
            foreach (var symbol in symbols) {
                this.customSymbols.append(this.AddData(SECReport8K, symbol, Resolution.Daily).Symbol);
            }
            return symbols;
        }
        
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested && this.Transactions.GetOpenOrders().Count == 0) {
                var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
                this.SetHoldings(aapl, 0.5);
            }
            foreach (var customSymbol in this.customSymbols) {
                if (!this.ActiveSecurities.ContainsKey(customSymbol.Underlying)) {
                    throw new Exception("Custom data undelrying ({customSymbol.Underlying}) Symbol was not found in active securities");
                }
            }
        }
    }
}
