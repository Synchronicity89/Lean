namespace AltData {
    
    using AddReference = clr.AddReference;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class SECReport8KAlgorithm {
        
        static SECReport8KAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Algorithm.Framework");
            AddReference("QuantConnect.Common");
        }
        
        public class SECReport8KAlgorithm
            : QCAlgorithm {
            
            public virtual object Initialize() {
                this.SetStartDate(2019, 1, 1);
                this.SetEndDate(2019, 8, 21);
                this.SetCash(100000);
                this.UniverseSettings.Resolution = Resolution.Minute;
                this.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(this.CoarseSelector));
                // Request underlying equity data.
                var ibm = this.AddEquity("IBM", Resolution.Minute).Symbol;
                // Add news data for the underlying IBM asset
                var earningsFiling = this.AddData(SECReport10Q, ibm, Resolution.Daily).Symbol;
                // Request 120 days of history with the SECReport10Q IBM custom data Symbol
                var history = this.History(SECReport10Q, earningsFiling, 120, Resolution.Daily);
                // Count the number of items we get from our history request
                this.Debug("We got {len(history)} items from our history request");
            }
            
            public virtual object CoarseSelector(object coarse) {
                // Add SEC data from the filtered coarse selection
                var symbols = (from i in coarse
                    where i.HasFundamentalData && i.DollarVolume > 50000000
                    select i.Symbol).ToList()[::10];
                foreach (var symbol in symbols) {
                    this.AddData(SECReport8K, symbol);
                }
                return symbols;
            }
            
            public virtual object OnData(object data) {
                // Store the symbols we want to long in a list
                // so that we can have an equal-weighted portfolio
                var longEquitySymbols = new List<object>();
                // Get all SEC data and loop over it
                foreach (var report in data.Get(SECReport8K).Values) {
                    // Get the length of all contents contained within the report
                    var reportTextLength = (from i in report.Report.Documents
                        select i.Text.Count).ToList().Sum();
                    if (reportTextLength > 20000) {
                        longEquitySymbols.append(report.Symbol.Underlying);
                    }
                }
                foreach (var equitySymbol in longEquitySymbols) {
                    this.SetHoldings(equitySymbol, 1.0 / longEquitySymbols.Count);
                }
            }
            
            public virtual object OnSecuritiesChanged(object changes) {
                foreach (var r in changes.RemovedSecurities) {
                    // If removed from the universe, liquidate and remove the custom data from the algorithm
                    this.Liquidate(r.Symbol);
                    this.RemoveSecurity(Symbol.CreateBase(SECReport8K, r.Symbol, Market.USA));
                }
            }
        }
    }
}
