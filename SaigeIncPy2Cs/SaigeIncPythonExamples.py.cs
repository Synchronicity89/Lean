
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class SaigeIncPythonExamples {
    
    static SaigeIncPythonExamples() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateOptionsAlgorithm
        : QCAlgorithm {
        
        public object option_symbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2016, 1, 1);
            this.SetEndDate(2016, 1, 10);
            this.SetCash(100000);
            var option = this.AddOption("GOOG");
            this.option_symbol = option.Symbol;
            // set our strike/expiry filter for this option chain
            option.SetFilter(-2, +2, new timedelta(0), new timedelta(180));
            // use the underlying equity as the benchmark
            this.SetBenchmark("GOOG");
        }
        
        public virtual object OnData(object slice) {
            if (this.Portfolio.Invested) {
                return;
            }
            foreach (var kvp in slice.OptionChains) {
                if (kvp.Key != this.option_symbol) {
                    continue;
                }
                var chain = kvp.Value;
                // we sort the contracts to find at the money (ATM) contract with farthest expiration
                var contracts = chain.OrderBy(x => abs(chain.Underlying.Price - x.Strike)).ToList().OrderByDescending(x => x.Expiry).ToList().OrderByDescending(x => x.Right).ToList();
                // if found, trade it
                if (contracts.Count == 0) {
                    continue;
                }
                var symbol = contracts[0].Symbol;
                this.MarketOrder(symbol, 1);
                this.MarketOnCloseOrder(symbol, -1);
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
