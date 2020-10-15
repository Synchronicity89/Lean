
using AddReference = clr.AddReference;

using d = @decimal;

using System.Linq;

public static class BasicTemplateCryptoAlgorithm {
    
    static BasicTemplateCryptoAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateCryptoAlgorithm
        : QCAlgorithm {
        
        public object fast;
        
        public object slow;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2018, 4, 4);
            this.SetEndDate(2018, 4, 4);
            // Although typically real brokerages as GDAX only support a single account currency,
            // here we add both USD and EUR to demonstrate how to handle non-USD account currencies.
            // Set Strategy Cash (USD)
            this.SetCash(10000);
            // Set Strategy Cash (EUR)
            // EUR/USD conversion rate will be updated dynamically
            this.SetCash("EUR", 10000);
            // Add some coins as initial holdings
            // When connected to a real brokerage, the amount specified in SetCash
            // will be replaced with the amount in your actual account.
            this.SetCash("BTC", 1);
            this.SetCash("ETH", 5);
            this.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
            // You can uncomment the following lines when live trading with GDAX,
            // to ensure limit orders will only be posted to the order book and never executed as a taker (incurring fees).
            // Please note this statement has no effect in backtesting or paper trading.
            // self.DefaultOrderProperties = GDAXOrderProperties()
            // self.DefaultOrderProperties.PostOnly = True
            // Find more symbols here: http://quantconnect.com/data
            this.AddCrypto("BTCUSD", Resolution.Minute);
            this.AddCrypto("ETHUSD", Resolution.Minute);
            this.AddCrypto("BTCEUR", Resolution.Minute);
            var symbol = this.AddCrypto("LTCUSD", Resolution.Minute).Symbol;
            // create two moving averages
            this.fast = this.EMA(symbol, 30, Resolution.Minute);
            this.slow = this.EMA(symbol, 60, Resolution.Minute);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            object usdTotal;
            object quantity;
            object limitPrice;
            // Note: all limit orders in this algorithm will be paying taker fees,
            // they shouldn't, but they do (for now) because of this issue:
            // https://github.com/QuantConnect/Lean/issues/1852
            if (this.Time.hour == 1 && this.Time.minute == 0) {
                // Sell all ETH holdings with a limit order at 1% above the current price
                limitPrice = round(this.Securities["ETHUSD"].Price * d.Decimal(1.01), 2);
                quantity = this.Portfolio.CashBook["ETH"].Amount;
                this.LimitOrder("ETHUSD", -quantity, limitPrice);
            } else if (this.Time.hour == 2 && this.Time.minute == 0) {
                // Submit a buy limit order for BTC at 5% below the current price
                usdTotal = this.Portfolio.CashBook["USD"].Amount;
                limitPrice = round(this.Securities["BTCUSD"].Price * d.Decimal(0.95), 2);
                // use only half of our total USD
                quantity = usdTotal * d.Decimal(0.5) / limitPrice;
                this.LimitOrder("BTCUSD", quantity, limitPrice);
            } else if (this.Time.hour == 2 && this.Time.minute == 1) {
                // Get current USD available, subtracting amount reserved for buy open orders
                usdTotal = this.Portfolio.CashBook["USD"].Amount;
                var usdReserved = (from x in (from x in this.Transactions.GetOpenOrders()
                    where x.Direction == OrderDirection.Buy && x.Type == OrderType.Limit && (x.Symbol.Value == "BTCUSD" || x.Symbol.Value == "ETHUSD")
                    select x).ToList()
                    select (x.Quantity * x.LimitPrice)).Sum();
                var usdAvailable = usdTotal - usdReserved;
                this.Debug("usdAvailable: {}".format(usdAvailable));
                // Submit a marketable buy limit order for ETH at 1% above the current price
                limitPrice = round(this.Securities["ETHUSD"].Price * d.Decimal(1.01), 2);
                // use all of our available USD
                quantity = usdAvailable / limitPrice;
                // this order will be rejected (for now) because of this issue:
                // https://github.com/QuantConnect/Lean/issues/1852
                this.LimitOrder("ETHUSD", quantity, limitPrice);
                // use only half of our available USD
                quantity = usdAvailable * d.Decimal(0.5) / limitPrice;
                this.LimitOrder("ETHUSD", quantity, limitPrice);
            } else if (this.Time.hour == 11 && this.Time.minute == 0) {
                // Liquidate our BTC holdings (including the initial holding)
                this.SetHoldings("BTCUSD", 0);
            } else if (this.Time.hour == 12 && this.Time.minute == 0) {
                // Submit a market buy order for 1 BTC using EUR
                this.Buy("BTCEUR", 1);
                // Submit a sell limit order at 10% above market price
                limitPrice = round(this.Securities["BTCEUR"].Price * d.Decimal(1.1), 2);
                this.LimitOrder("BTCEUR", -1, limitPrice);
            } else if (this.Time.hour == 13 && this.Time.minute == 0) {
                // Cancel the limit order if not filled
                this.Transactions.CancelOpenOrders("BTCEUR");
            } else if (this.Time.hour > 13) {
                // To include any initial holdings, we read the LTC amount from the cashbook
                // instead of using Portfolio["LTCUSD"].Quantity
                if (this.fast > this.slow) {
                    if (this.Portfolio.CashBook["LTC"].Amount == 0) {
                        this.Buy("LTCUSD", 10);
                    }
                } else if (this.Portfolio.CashBook["LTC"].Amount > 0) {
                    // The following two statements currently behave differently if we have initial holdings:
                    // https://github.com/QuantConnect/Lean/issues/1860
                    this.Liquidate("LTCUSD");
                    // self.SetHoldings("LTCUSD", 0)
                }
            }
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            this.Debug("{} {}".format(this.Time, orderEvent.ToString()));
        }
        
        public virtual object OnEndOfAlgorithm() {
            this.Log("{} - TotalPortfolioValue: {}".format(this.Time, this.Portfolio.TotalPortfolioValue));
            this.Log("{} - CashBook: {}".format(this.Time, this.Portfolio.CashBook));
        }
    }
}
