
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class OptionChainConsistencyRegressionAlgorithm {
    
    static OptionChainConsistencyRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class OptionChainConsistencyRegressionAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetCash(10000);
            this.SetStartDate(2015, 12, 24);
            this.SetEndDate(2015, 12, 24);
            var option = this.AddOption("GOOG");
            // set our strike/expiry filter for this option chain
            option.SetFilter(this.UniverseFunc);
            this.SetBenchmark("GOOG");
        }
        
        public virtual object OnData(object slice) {
            if (this.Portfolio.Invested) {
                return;
            }
            foreach (var kvp in slice.OptionChains) {
                var chain = kvp.Value;
                foreach (var o in chain) {
                    if (!this.Securities.ContainsKey(o.Symbol)) {
                        this.Log("Inconsistency found: option chains contains contract {0} that is not available in securities manager and not available for trading".format(o.Symbol.Value));
                    }
                }
                var contracts = chain.Where(x => x.Expiry.date() == this.Time.date() && x.Strike < chain.Underlying.Price && x.Right == OptionRight.Call).ToList();
                var sorted_contracts = contracts.OrderByDescending(x => x.Strike).ToList();
                if (sorted_contracts.Count > 2) {
                    this.MarketOrder(sorted_contracts[2].Symbol, 1);
                    this.MarketOnCloseOrder(sorted_contracts[2].Symbol, -1);
                }
            }
        }
        
        // set our strike/expiry filter for this option chain
        public virtual object UniverseFunc(object universe) {
            return universe.IncludeWeeklys().Strikes(-2, 2).Expiration(new timedelta(0), new timedelta(10));
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
