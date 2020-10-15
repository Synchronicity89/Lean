
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class OptionSplitRegressionAlgorithm {
    
    static OptionSplitRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class OptionSplitRegressionAlgorithm
        : QCAlgorithm {
        
        public object contract;
        
        public virtual object Initialize() {
            // this test opens position in the first day of trading, lives through stock split (7 for 1),
            // and closes adjusted position on the second day
            this.SetCash(1000000);
            this.SetStartDate(2014, 6, 6);
            this.SetEndDate(2014, 6, 9);
            var option = this.AddOption("AAPL");
            // set our strike/expiry filter for this option chain
            option.SetFilter(this.UniverseFunc);
            this.SetBenchmark("AAPL");
            this.contract = null;
        }
        
        public virtual object OnData(object slice) {
            if (!this.Portfolio.Invested) {
                if (this.Time.hour > 9 && this.Time.minute > 0) {
                    foreach (var kvp in slice.OptionChains) {
                        var chain = kvp.Value;
                        var contracts = chain.Where(x => x.Strike == 650 && x.Right == OptionRight.Call).ToList();
                        var sorted_contracts = contracts.OrderBy(x => x.Expiry).ToList();
                    }
                    if (sorted_contracts.Count > 1) {
                        this.contract = sorted_contracts[1];
                        this.Buy(this.contract.Symbol, 1);
                    }
                }
            } else if (this.Time.day > 6 && this.Time.hour > 14 && this.Time.minute > 0) {
                this.Liquidate();
            }
            if (this.Portfolio.Invested) {
                var options_hold = (from x in this.Portfolio.Securities
                    where x.Value.Holdings.AbsoluteQuantity != 0
                    select x).ToList();
                var holdings = options_hold[0].Value.Holdings.AbsoluteQuantity;
                if (this.Time.day == 6 && holdings != 1) {
                    this.Log("Expected position quantity of 1 but was {0}".format(holdings));
                }
                if (this.Time.day == 9 && holdings != 7) {
                    this.Log("Expected position quantity of 7 but was {0}".format(holdings));
                }
            }
        }
        
        // set our strike/expiry filter for this option chain
        public virtual object UniverseFunc(object universe) {
            return universe.IncludeWeeklys().Strikes(-2, 2).Expiration(new timedelta(0), new timedelta(365 * 2));
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
