
using clr;

using np = numpy;

using torch;

using F = torch.nn.functional;

using System.Collections.Generic;

using System.Linq;

public static class PytorchNeuralNetworkAlgorithm {
    
    static PytorchNeuralNetworkAlgorithm() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Common");
    }
    
    public class PytorchNeuralNetworkAlgorithm
        : QCAlgorithm {
        
        public Dictionary<object, object> buy_prices;
        
        public int lookback;
        
        public Dictionary<object, object> prices_x;
        
        public Dictionary<object, object> prices_y;
        
        public Dictionary<object, object> sell_prices;
        
        public List<object> symbols;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 8);
            this.SetCash(100000);
            // add symbol
            var spy = this.AddEquity("SPY", Resolution.Minute);
            this.symbols = new List<object> {
                spy.Symbol
            };
            this.lookback = 30;
            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", 28), this.NetTrain);
            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", 30), this.Trade);
        }
        
        public virtual object NetTrain() {
            // Daily historical data is used to train the machine learning model
            var history = this.History(this.symbols, this.lookback + 1, Resolution.Daily);
            // dicts that store prices for training
            this.prices_x = new Dictionary<object, object> {
            };
            this.prices_y = new Dictionary<object, object> {
            };
            // dicts that store prices for sell and buy
            this.sell_prices = new Dictionary<object, object> {
            };
            this.buy_prices = new Dictionary<object, object> {
            };
            foreach (var symbol in this.symbols) {
                if (!history.empty) {
                    // x: preditors; y: response
                    this.prices_x[symbol] = history.loc[symbol.Value]["open"].ToList()[:: - 1];
                    this.prices_y[symbol] = history.loc[symbol.Value]["open"].ToList()[1];
                }
            }
            foreach (var symbol in this.symbols) {
                // if this symbol has historical data
                if (this.prices_x.Contains(symbol)) {
                    var net = new Net(n_feature: 1, n_hidden: 10, n_output: 1);
                    var optimizer = torch.optim.SGD(net.parameters(), lr: 0.2);
                    var loss_func = torch.nn.MSELoss();
                    foreach (var t in range(200)) {
                        // Get data and do preprocessing
                        var x = torch.from_numpy(np.array(this.prices_x[symbol])).float();
                        var y = torch.from_numpy(np.array(this.prices_y[symbol])).float();
                        // unsqueeze data (see pytorch doc for details)
                        x = x.unsqueeze(1);
                        y = y.unsqueeze(1);
                        var prediction = net(x);
                        var loss = loss_func(prediction, y);
                        optimizer.zero_grad();
                        loss.backward();
                        optimizer.step();
                    }
                }
                // Follow the trend    
                this.buy_prices[symbol] = net(y)[-1] + np.std(y.data.numpy());
                this.sell_prices[symbol] = net(y)[-1] - np.std(y.data.numpy());
            }
        }
        
        //  
        //         Enter or exit positions based on relationship of the open price of the current bar and the prices defined by the machine learning model.
        //         Liquidate if the open price is below the sell price and buy if the open price is above the buy price 
        //         
        public virtual object Trade() {
            foreach (var holding in this.Portfolio.Values) {
                if (this.CurrentSlice[holding.Symbol].Open < this.sell_prices[holding.Symbol] && holding.Invested) {
                    this.Liquidate(holding.Symbol);
                }
                if (this.CurrentSlice[holding.Symbol].Open > this.buy_prices[holding.Symbol] && !holding.Invested) {
                    this.SetHoldings(holding.Symbol, 1 / this.symbols.Count);
                }
            }
        }
    }
    
    public class Net
        : torch.nn.Module {
        
        public object hidden;
        
        public object predict;
        
        public Net(object n_feature, object n_hidden, object n_output) {
            this.hidden = torch.nn.Linear(n_feature, n_hidden);
            this.predict = torch.nn.Linear(n_hidden, n_output);
        }
        
        public virtual object forward(object x) {
            x = F.relu(this.hidden(x));
            x = this.predict(x);
            return x;
        }
    }
}
