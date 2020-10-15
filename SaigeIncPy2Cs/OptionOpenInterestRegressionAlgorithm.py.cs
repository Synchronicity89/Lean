
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

public static class OptionOpenInterestRegressionAlgorithm {
    
    static OptionOpenInterestRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class OptionOpenInterestRegressionAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetCash(1000000);
            this.SetStartDate(2014, 6, 5);
            this.SetEndDate(2014, 6, 6);
            var option = this.AddOption("TWX");
            // set our strike/expiry filter for this option chain
            option.SetFilter(-10, 10, new timedelta(0), new timedelta(365 * 2));
            // use the underlying equity as the benchmark
            this.SetBenchmark("TWX");
        }
        
        public virtual object OnData(object slice) {
            if (!this.Portfolio.Invested) {
                foreach (var chain in slice.OptionChains) {
                    foreach (var contract in chain.Value) {
                        if (float(contract.Symbol.ID.StrikePrice) == 72.5 && contract.Symbol.ID.OptionRight == OptionRight.Call && contract.Symbol.ID.Date == new datetime(2016, 1, 15)) {
                            if (slice.Time.date() == new datetime(2014, 6, 5).date() && contract.OpenInterest != 50) {
                                throw new ValueError("Regression test failed: current open interest was not correctly loaded and is not equal to 50");
                            }
                            if (slice.Time.date() == new datetime(2014, 6, 6).date() && contract.OpenInterest != 70) {
                                throw new ValueError("Regression test failed: current open interest was not correctly loaded and is not equal to 70");
                            }
                            if (slice.Time.date() == new datetime(2014, 6, 6).date()) {
                                this.MarketOrder(contract.Symbol, 1);
                                this.MarketOnCloseOrder(contract.Symbol, -1);
                            }
                        }
                    }
                }
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Log(orderEvent.ToString());
        }
    }
}
