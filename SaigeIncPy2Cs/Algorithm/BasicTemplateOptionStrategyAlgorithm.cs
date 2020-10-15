
using AddReference = clr.AddReference;

using OptionStrategies = QuantConnect.Securities.Option.OptionStrategies;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class BasicTemplateOptionStrategyAlgorithm {
    
    static BasicTemplateOptionStrategyAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateOptionStrategyAlgorithm
        : QCAlgorithm {
        
        public object option_symbol;
        
        public virtual object Initialize() {
            // Set the cash we'd like to use for our backtest
            this.SetCash(1000000);
            // Start and end dates for the backtest.
            this.SetStartDate(2015, 12, 24);
            this.SetEndDate(2015, 12, 24);
            // Add assets you'd like to see
            var option = this.AddOption("GOOG");
            this.option_symbol = option.Symbol;
            // set our strike/expiry filter for this option chain
            option.SetFilter(-2, +2, new timedelta(0), new timedelta(180));
            // use the underlying equity as the benchmark
            this.SetBenchmark("GOOG");
        }
        
        public virtual object OnData(object slice) {
            if (!this.Portfolio.Invested) {
                foreach (var kvp in slice.OptionChains) {
                    var chain = kvp.Value;
                    var contracts = chain.OrderBy(x => abs(chain.Underlying.Price - x.Strike)).ToList().OrderBy(x => x.Expiry).ToList();
                    if (contracts.Count == 0) {
                        continue;
                    }
                    var atmStraddle = contracts[0];
                    if (atmStraddle != null) {
                        this.Sell(OptionStrategies.Straddle(this.option_symbol, atmStraddle.Strike, atmStraddle.Expiry), 2);
                    }
                }
            } else {
                this.Liquidate();
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
