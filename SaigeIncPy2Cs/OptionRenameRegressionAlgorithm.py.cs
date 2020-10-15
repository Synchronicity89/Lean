
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class OptionRenameRegressionAlgorithm {
    
    static OptionRenameRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class OptionRenameRegressionAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetCash(1000000);
            this.SetStartDate(2013, 6, 28);
            this.SetEndDate(2013, 7, 2);
            var option = this.AddOption("FOXA");
            // set our strike/expiry filter for this option chain
            option.SetFilter(-1, 1, new timedelta(0), new timedelta(3650));
            // use the underlying equity as the benchmark
            this.SetBenchmark("FOXA");
        }
        
        //  Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        //         <param name="slice">The current slice of data keyed by symbol string</param> 
        public virtual object OnData(object slice) {
            object contract;
            object contracts;
            object chain;
            if (!this.Portfolio.Invested) {
                foreach (var kvp in slice.OptionChains) {
                    chain = kvp.Value;
                    if (this.Time.day == 28 && this.Time.hour > 9 && this.Time.minute > 0) {
                        contracts = (from i in chain.OrderBy(x => x.Expiry).ToList()
                            where i.Right == OptionRight.Call && i.Strike == 33 && i.Expiry.date() == datetime(2013, 8, 17).date()
                            select i).ToList();
                        if (contracts) {
                            // Buying option
                            contract = contracts[0];
                            this.Buy(contract.Symbol, 1);
                            // Buy the undelying stock
                            var underlyingSymbol = contract.Symbol.Underlying;
                            this.Buy(underlyingSymbol, 100);
                            // check
                            if (float(contract.AskPrice) != 1.1) {
                                throw new ValueError("Regression test failed: current ask price was not loaded from NWSA backtest file and is not $1.1");
                            }
                        }
                    }
                }
            } else if (this.Time.day == 2 && this.Time.hour > 14 && this.Time.minute > 0) {
                foreach (var kvp in slice.OptionChains) {
                    chain = kvp.Value;
                    this.Liquidate();
                    contracts = (from i in chain.OrderBy(x => x.Expiry).ToList()
                        where i.Right == OptionRight.Call && i.Strike == 33 && i.Expiry.date() == datetime(2013, 8, 17).date()
                        select i).ToList();
                }
                if (contracts) {
                    contract = contracts[0];
                    this.Log("Bid Price" + contract.BidPrice.ToString());
                    if (float(contract.BidPrice) != 0.05) {
                        throw new ValueError("Regression test failed: current bid price was not loaded from FOXA file and is not $0.05");
                    }
                }
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
