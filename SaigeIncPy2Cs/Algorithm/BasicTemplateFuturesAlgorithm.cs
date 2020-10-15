
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class BasicTemplateFuturesAlgorithm {
    
    static BasicTemplateFuturesAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateFuturesAlgorithm
        : QCAlgorithm {
        
        public object contractSymbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 8);
            this.SetEndDate(2013, 10, 10);
            this.SetCash(1000000);
            this.contractSymbol = null;
            // Subscribe and set our expiry filter for the futures chain
            var futureES = this.AddFuture(Futures.Indices.SP500EMini);
            futureES.SetFilter(new timedelta(0), new timedelta(182));
            var futureGC = this.AddFuture(Futures.Metals.Gold);
            futureGC.SetFilter(new timedelta(0), new timedelta(182));
            var benchmark = this.AddEquity("SPY");
            this.SetBenchmark(benchmark.Symbol);
        }
        
        public virtual object OnData(object slice) {
            if (!this.Portfolio.Invested) {
                foreach (var chain in slice.FutureChains) {
                    // Get contracts expiring no earlier than in 90 days
                    var contracts = chain.Value.Where(x => x.Expiry > this.Time + timedelta(90)).ToList().ToList();
                    // if there is any contract, trade the front contract
                    if (contracts.Count == 0) {
                        continue;
                    }
                    var front = contracts.OrderByDescending(x => x.Expiry).ToList()[0];
                    this.contractSymbol = front.Symbol;
                    this.MarketOrder(front.Symbol, 1);
                }
            } else {
                this.Liquidate();
            }
        }
        
        public virtual object OnEndOfAlgorithm() {
            // Get the margin requirements
            var buyingPowerModel = this.Securities[this.contractSymbol].BuyingPowerModel;
            var name = type(buyingPowerModel).@__name__;
            if (name != "FutureMarginModel") {
                throw new Exception("Invalid buying power model. Found: {name}. Expected: FutureMarginModel");
            }
            var initialMarginRequirement = buyingPowerModel.InitialMarginRequirement;
            var maintenanceMarginRequirement = buyingPowerModel.MaintenanceMarginRequirement;
        }
    }
}
