
using AddReference = clr.AddReference;

using OptionPriceModels = QuantConnect.Securities.Option.OptionPriceModels;

using timedelta = datetime.timedelta;

public static class BasicTemplateOptionsHistoryAlgorithm {
    
    static BasicTemplateOptionsHistoryAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    //  This example demonstrates how to get access to options history for a given underlying equity security.
    public class BasicTemplateOptionsHistoryAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            // this test opens position in the first day of trading, lives through stock split (7 for 1), and closes adjusted position on the second day
            this.SetStartDate(2015, 12, 24);
            this.SetEndDate(2015, 12, 24);
            this.SetCash(1000000);
            var option = this.AddOption("GOOG");
            // add the initial contract filter 
            option.SetFilter(-2, 2, new timedelta(0), new timedelta(180));
            // set the pricing model for Greeks and volatility
            // find more pricing models https://www.quantconnect.com/lean/documentation/topic27704.html
            option.PriceModel = OptionPriceModels.CrankNicolsonFD();
            // set the warm-up period for the pricing model
            this.SetWarmUp(TimeSpan.FromDays(4));
            // set the benchmark to be the initial cash
            this.SetBenchmark(x => 1000000);
        }
        
        public virtual object OnData(object slice) {
            if (this.IsWarmingUp) {
                return;
            }
            if (!this.Portfolio.Invested) {
                foreach (var chain in slice.OptionChains) {
                    var volatility = this.Securities[chain.Key.Underlying].VolatilityModel.Volatility;
                    foreach (var contract in chain.Value) {
                        this.Log("{0},Bid={1} Ask={2} Last={3} OI={4} sigma={5:.3f} NPV={6:.3f} \
                              delta={7:.3f} gamma={8:.3f} vega={9:.3f} beta={10:.2f} theta={11:.2f} IV={12:.2f}".format(contract.Symbol.Value, contract.BidPrice, contract.AskPrice, contract.LastPrice, contract.OpenInterest, volatility, contract.TheoreticalPrice, contract.Greeks.Delta, contract.Greeks.Gamma, contract.Greeks.Vega, contract.Greeks.Rho, contract.Greeks.Theta / 365, contract.ImpliedVolatility));
                    }
                }
            }
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            foreach (var change in changes.AddedSecurities) {
                // only print options price
                if (change.Symbol.Value == "GOOG") {
                    return;
                }
                var history = this.History(change.Symbol, 10, Resolution.Minute).sort_index(level: "time", ascending: false)[::3];
                foreach (var _tup_1 in history.iterrows()) {
                    var index = _tup_1.Item1;
                    var row = _tup_1.Item2;
                    this.Log("History: " + index[3].ToString() + ": " + index[4].strftime("%m/%d/%Y %I:%M:%S %p") + " > " + row.close.ToString());
                }
            }
        }
    }
}
