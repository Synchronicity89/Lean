
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class BasicTemplateOptionsAlgorithm {
    
    static BasicTemplateOptionsAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateOptionsAlgorithm
        : QCAlgorithm {
        
        public object option_symbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2015, 12, 24);
            this.SetEndDate(2015, 12, 24);
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
            var chain = slice.OptionChains.GetValue(this.option_symbol);
            if (chain == null) {
                return;
            }
            // we sort the contracts to find at the money (ATM) contract with farthest expiration
            var contracts = chain.OrderBy(x => abs(chain.Underlying.Price - x.Strike)).ToList().OrderByDescending(x => x.Expiry).ToList().OrderByDescending(x => x.Right).ToList();
            // if found, trade it
            if (contracts.Count == 0) {
                return;
            }
            var symbol = contracts[0].Symbol;
            this.MarketOrder(symbol, 1);
            this.MarketOnCloseOrder(symbol, -1);
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
