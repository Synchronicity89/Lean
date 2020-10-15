
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class OptionExerciseAssignRegressionAlgorithm {
    
    static OptionExerciseAssignRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class OptionExerciseAssignRegressionAlgorithm
        : QCAlgorithm {
        
        public bool _assignedOption;
        
        public virtual object Initialize() {
            this.SetCash(100000);
            this.SetStartDate(2015, 12, 24);
            this.SetEndDate(2015, 12, 24);
            var option = this.AddOption("GOOG");
            // set our strike/expiry filter for this option chain
            option.SetFilter(this.UniverseFunc);
            this.SetBenchmark("GOOG");
            this._assignedOption = false;
        }
        
        public virtual object OnData(object slice) {
            if (this.Portfolio.Invested) {
                return;
            }
            foreach (var kvp in slice.OptionChains) {
                var chain = kvp.Value;
                // find the call options expiring today
                var contracts = chain.Where(x => x.Expiry.date() == this.Time.date() && x.Strike < chain.Underlying.Price && x.Right == OptionRight.Call).ToList();
                // sorted the contracts by their strikes, find the second strike under market price 
                var sorted_contracts = contracts.OrderByDescending(x => x.Strike).ToList()[::2];
                if (sorted_contracts) {
                    this.MarketOrder(sorted_contracts[0].Symbol, 1);
                    this.MarketOrder(sorted_contracts[1].Symbol, -1);
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
        
        public virtual object OnAssignmentOrderEvent(object assignmentEvent) {
            this.Log(assignmentEvent.ToString());
            this._assignedOption = true;
        }
    }
}
