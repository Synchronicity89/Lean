
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

public static class BasicTemplateFuturesHistoryAlgorithm {
    
    static BasicTemplateFuturesHistoryAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateFuturesHistoryAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 8);
            this.SetEndDate(2013, 10, 9);
            this.SetCash(1000000);
            // Subscribe and set our expiry filter for the futures chain
            // find the front contract expiring no earlier than in 90 days
            var futureES = this.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute);
            futureES.SetFilter(new timedelta(0), new timedelta(182));
            var futureGC = this.AddFuture(Futures.Metals.Gold, Resolution.Minute);
            futureGC.SetFilter(new timedelta(0), new timedelta(182));
            this.SetBenchmark(x => 1000000);
        }
        
        public virtual object OnData(object slice) {
            if (this.Portfolio.Invested) {
                return;
            }
            foreach (var chain in slice.FutureChains) {
                foreach (var contract in chain.Value) {
                    this.Log("{0},Bid={1} Ask={2} Last={3} OI={4}".format(contract.Symbol.Value, contract.BidPrice, contract.AskPrice, contract.LastPrice, contract.OpenInterest));
                }
            }
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            foreach (var change in changes.AddedSecurities) {
                var history = this.History(change.Symbol, 10, Resolution.Minute).sort_index(level: "time", ascending: false)[::3];
                foreach (var _tup_1 in history.iterrows()) {
                    var index = _tup_1.Item1;
                    var row = _tup_1.Item2;
                    this.Log("History: " + index[1].ToString() + ": " + index[2].strftime("%m/%d/%Y %I:%M:%S %p") + " > " + row.close.ToString());
                }
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            // Order fill event handler. On an order fill update the resulting information is passed to this method.
            // Order event details containing details of the events
            this.Log(orderEvent.ToString());
        }
    }
}
