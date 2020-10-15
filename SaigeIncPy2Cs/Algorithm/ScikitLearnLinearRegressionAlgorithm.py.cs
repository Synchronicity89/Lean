
using clr;

using np = numpy;

using LinearRegression = sklearn.linear_model.LinearRegression;

using System.Collections.Generic;

using System.Linq;

public static class ScikitLearnLinearRegressionAlgorithm {
    
    static ScikitLearnLinearRegressionAlgorithm() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Common");
    }
    
    public class ScikitLearnLinearRegressionAlgorithm
        : QCAlgorithm {
        
        public int lookback;
        
        public Dictionary<object, object> prices;
        
        public Dictionary<object, object> slopes;
        
        public List<object> symbols;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 8);
            this.lookback = 30;
            this.SetCash(100000);
            var spy = this.AddEquity("SPY", Resolution.Minute);
            this.symbols = new List<object> {
                spy.Symbol
            };
            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", 28), this.Regression);
            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", 30), this.Trade);
        }
        
        public virtual object Regression() {
            // Daily historical data is used to train the machine learning model
            var history = this.History(this.symbols, this.lookback, Resolution.Daily);
            // price dictionary:    key: symbol; value: historical price
            this.prices = new Dictionary<object, object> {
            };
            // slope dictionary:    key: symbol; value: slope
            this.slopes = new Dictionary<object, object> {
            };
            foreach (var symbol in this.symbols) {
                if (!history.empty) {
                    // get historical open price
                    this.prices[symbol] = history.loc[symbol.Value]["open"].ToList();
                }
            }
            // A is the design matrix
            var A = range(this.lookback + 1);
            foreach (var symbol in this.symbols) {
                if (this.prices.Contains(symbol)) {
                    // response
                    var Y = this.prices[symbol];
                    // features
                    var X = np.column_stack(new List<List<int>> {
                        np.ones(A.Count),
                        A
                    });
                    // data preparation
                    var length = min(X.Count, Y.Count);
                    X = X[-length];
                    Y = Y[-length];
                    A = A[-length];
                    // fit the linear regression
                    var reg = LinearRegression().fit(X, Y);
                    // run linear regression y = ax + b
                    var b = reg.intercept_;
                    var a = reg.coef_[1];
                    // store slopes for symbols
                    this.slopes[symbol] = a / b;
                }
            }
        }
        
        public virtual object Trade() {
            // if there is no open price
            if (!this.prices) {
                return;
            }
            var thod_buy = 0.001;
            var thod_liquidate = -0.001;
            foreach (var holding in this.Portfolio.Values) {
                var slope = this.slopes[holding.Symbol];
                // liquidate when slope smaller than thod_liquidate
                if (holding.Invested && slope < thod_liquidate) {
                    this.Liquidate(holding.Symbol);
                }
            }
            foreach (var symbol in this.symbols) {
                // buy when slope larger than thod_buy
                if (this.slopes[symbol] > thod_buy) {
                    this.SetHoldings(symbol, 1 / this.symbols.Count);
                }
            }
        }
    }
}
