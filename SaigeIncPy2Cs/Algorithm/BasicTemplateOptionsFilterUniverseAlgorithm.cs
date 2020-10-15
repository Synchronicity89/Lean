
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class BasicTemplateOptionsFilterUniverseAlgorithm {
    
    static BasicTemplateOptionsFilterUniverseAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateOptionsFilterUniverseAlgorithm
        : QCAlgorithm {
        
        public object option_symbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2015, 12, 16);
            this.SetEndDate(2015, 12, 24);
            this.SetCash(100000);
            var option = this.AddOption("GOOG");
            this.option_symbol = option.Symbol;
            // set our strike/expiry filter for this option chain
            option.SetFilter(-10, 10, new timedelta(0), new timedelta(10));
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
                // find the call options expiring today
                var contracts = (from i in chain
                    where i.Right == OptionRight.Call && i.Expiry.date() == this.Time.date()
                    select i).ToList();
                // sorted the contracts by their strike, find the second strike under market price 
                var sorted_contracts = (from i in contracts.OrderByDescending(x => x.Strike).ToList()
                    where i.Strike < chain.Underlying.Price
                    select i).ToList();
                // if found, trade it
                if (sorted_contracts.Count == 0) {
                    this.Log("No call contracts expiring today");
                    return;
                }
                this.MarketOrder(sorted_contracts[1].Symbol, 1);
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            // Order fill event handler. On an order fill update the resulting information is passed to this method.
            // <param name="orderEvent">Order event details containing details of the evemts</param>
            this.Log(orderEvent.ToString());
        }
    }
}
