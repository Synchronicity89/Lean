
using clr;

using np = numpy;

using Sequential = keras.models.Sequential;

using Dense = keras.layers.Dense;

using Activation = keras.layers.Activation;

using SGD = keras.optimizers.SGD;

using System.Collections.Generic;

using System.Linq;

public static class KerasNeuralNetworkAlgorithm {
    
    static KerasNeuralNetworkAlgorithm() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Common");
    }
    
    public class KerasNeuralNetworkAlgorithm
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
            var spy = this.AddEquity("SPY", Resolution.Minute);
            this.symbols = new List<object> {
                spy.Symbol
            };
            this.lookback = 30;
            this.Schedule.On(this.DateRules.Every(DayOfWeek.Monday), this.TimeRules.AfterMarketOpen("SPY", 28), this.NetTrain);
            this.Schedule.On(this.DateRules.Every(DayOfWeek.Monday), this.TimeRules.AfterMarketOpen("SPY", 30), this.Trade);
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
                    // x: pridictors; y: response
                    this.prices_x[symbol] = history.loc[symbol.Value]["open"].ToList()[:: - 1];
                    this.prices_y[symbol] = history.loc[symbol.Value]["open"].ToList()[1];
                }
            }
            foreach (var symbol in this.symbols) {
                if (this.prices_x.Contains(symbol)) {
                    // convert the original data to np array for fitting the keras NN model
                    var x_data = np.array(this.prices_x[symbol]);
                    var y_data = np.array(this.prices_y[symbol]);
                    // build a neural network from the 1st layer to the last layer
                    var model = Sequential();
                    model.add(Dense(10, input_dim: 1));
                    model.add(Activation("relu"));
                    model.add(Dense(1));
                    var sgd = SGD(lr: 0.01);
                    // choose loss function and optimizing method
                    model.compile(loss: "mse", optimizer: sgd);
                    // pick an iteration number large enough for convergence 
                    foreach (var step in range(701)) {
                        // training the model
                        var cost = model.train_on_batch(x_data, y_data);
                    }
                }
                // get the final predicted price 
                var y_pred_final = model.predict(y_data)[0][-1];
                // Follow the trend
                this.buy_prices[symbol] = y_pred_final + np.std(y_data);
                this.sell_prices[symbol] = y_pred_final - np.std(y_data);
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
}
