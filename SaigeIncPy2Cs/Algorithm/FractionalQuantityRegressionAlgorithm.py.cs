
using AddReference = clr.AddReference;

using DateTimeZone = NodaTime.DateTimeZone;

using timedelta = datetime.timedelta;

using floor = math.floor;

public static class FractionalQuantityRegressionAlgorithm {
    
    static FractionalQuantityRegressionAlgorithm() {
        AddReference("System");
        AddReference("NodaTime");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class FractionalQuantityRegressionAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2015, 11, 12);
            this.SetEndDate(2016, 4, 1);
            this.SetCash(100000);
            this.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
            this.SetTimeZone(DateTimeZone.Utc);
            var security = this.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Daily, Market.GDAX, false, 3.3, true);
            //## The default buying power model for the Crypto security type is now CashBuyingPowerModel.
            //## Since this test algorithm uses leverage we need to set a buying power model with margin.
            security.SetBuyingPowerModel(SecurityMarginModel(3.3));
            var con = TradeBarConsolidator(new timedelta(1));
            this.SubscriptionManager.AddConsolidator("BTCUSD", con);
            con.DataConsolidated += this.DataConsolidated;
            this.SetBenchmark(security.Symbol);
        }
        
        public virtual object DataConsolidated(object sender, object bar) {
            var quantity = floor((this.Portfolio.Cash + this.Portfolio.TotalFees) / abs(bar.Value + 1));
            var btc_qnty = float(this.Portfolio["BTCUSD"].Quantity);
            if (!this.Portfolio.Invested) {
                this.Order("BTCUSD", quantity);
            } else if (btc_qnty == quantity) {
                this.Order("BTCUSD", 0.1);
            } else if (btc_qnty == quantity + 0.1) {
                this.Order("BTCUSD", 0.01);
            } else if (btc_qnty == quantity + 0.11) {
                this.Order("BTCUSD", -0.02);
            } else if (btc_qnty == quantity + 0.09) {
                // should fail (below minimum order quantity)
                this.Order("BTCUSD", 1E-05);
                this.SetHoldings("BTCUSD", -2.0);
                this.SetHoldings("BTCUSD", 2.0);
                this.Quit();
            }
        }
    }
}
