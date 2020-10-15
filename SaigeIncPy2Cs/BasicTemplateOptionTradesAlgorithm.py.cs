
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class BasicTemplateOptionTradesAlgorithm {
    
    static BasicTemplateOptionTradesAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateOptionTradesAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2015, 12, 24);
            this.SetEndDate(2015, 12, 24);
            this.SetCash(100000);
            var option = this.AddOption("GOOG");
            // set our strike/expiry filter for this option chain
            option.SetFilter(-2, +2, new timedelta(0), new timedelta(30));
            // use the underlying equity as the benchmark
            this.SetBenchmark("GOOG");
        }
        
        public virtual object OnData(object slice) {
            if (!this.Portfolio.Invested) {
                foreach (var kvp in slice.OptionChains) {
                    var chain = kvp.Value;
                    // find the second call strike under market price expiring today
                    var contracts = chain.OrderBy(x => abs(chain.Underlying.Price - x.Strike)).ToList().OrderBy(x => x.Expiry).ToList();
                    if (contracts.Count == 0) {
                        continue;
                    }
                    if (contracts[0] != null) {
                        this.MarketOrder(contracts[0].Symbol, 1);
                    }
                }
            } else {
                this.Liquidate();
            }
            foreach (var kpv in slice.Bars) {
                this.Log("---> OnData: {0}, {1}, {2}".format(this.Time, kpv.Key.Value, kpv.Value.Close.ToString()));
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
