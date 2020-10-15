
using clr;

using np = numpy;

using tf = tensorflow;

using System.Collections.Generic;

using System.Linq;

public static class TensorFlowNeuralNetworkAlgorithm {
    
    static TensorFlowNeuralNetworkAlgorithm() {
        clr.AddReference("System");
        clr.AddReference("QuantConnect.Algorithm");
        clr.AddReference("QuantConnect.Common");
    }
    
    public class TensorFlowNeuralNetworkAlgorithm
        : QCAlgorithm {
        
        public int lookback;
        
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
        
        public virtual object add_layer(object inputs, object in_size, object out_size, object activation_function = null) {
            object outputs;
            // add one more layer and return the output of this layer
            // this is one NN with only one hidden layer
            var Weights = tf.Variable(tf.random_normal(new List<int> {
                in_size,
                out_size
            }));
            var biases = tf.Variable(tf.zeros(new List<int> {
                1,
                out_size
            }) + 0.1);
            var Wx_plus_b = tf.matmul(inputs, Weights) + biases;
            if (activation_function == null) {
                outputs = Wx_plus_b;
            } else {
                outputs = activation_function(Wx_plus_b);
            }
            return outputs;
        }
        
        public virtual object NetTrain() {
            // Daily historical data is used to train the machine learning model
            var history = this.History(this.symbols, this.lookback + 1, Resolution.Daily);
            // model: use prices_x to fit prices_y; key: symbol; value: according price
            this.prices_x = new Dictionary<object, object> {
            };
            this.prices_y = new Dictionary<object, object> {
            };
            // key: symbol; values: prices for sell or buy 
            this.sell_prices = new Dictionary<object, object> {
            };
            this.buy_prices = new Dictionary<object, object> {
            };
            foreach (var symbol in this.symbols) {
                if (!history.empty) {
                    // Daily historical data is used to train the machine learning model 
                    // use open prices to predict the next days'
                    this.prices_x[symbol] = history.loc[symbol.Value]["open"][:: - 1].ToList();
                    this.prices_y[symbol] = history.loc[symbol.Value]["open"][1].ToList();
                }
            }
            foreach (var symbol in this.symbols) {
                if (this.prices_x.Contains(symbol)) {
                    // create numpy array
                    var x_data = np.array(this.prices_x[symbol]).astype(np.float32).reshape(Tuple.Create(-1, 1));
                    var y_data = np.array(this.prices_y[symbol]).astype(np.float32).reshape(Tuple.Create(-1, 1));
                    // define placeholder for inputs to network
                    var xs = tf.placeholder(tf.float32, new List<object> {
                        null,
                        1
                    });
                    var ys = tf.placeholder(tf.float32, new List<object> {
                        null,
                        1
                    });
                    // add hidden layer
                    var l1 = this.add_layer(xs, 1, 10, activation_function: tf.nn.relu);
                    // add output layer
                    var prediction = this.add_layer(l1, 10, 1, activation_function: null);
                    // the error between prediciton and real data
                    var loss = tf.reduce_mean(tf.reduce_sum(tf.square(ys - prediction), reduction_indices: new List<int> {
                        1
                    }));
                    // use gradient descent and square error
                    var train_step = tf.train.GradientDescentOptimizer(0.1).minimize(loss);
                    // the following is precedure for tensorflow
                    var sess = tf.Session();
                    var init = tf.global_variables_initializer();
                    sess.run(init);
                    foreach (var i in range(200)) {
                        // training
                        sess.run(train_step, feed_dict: new Dictionary<object, object> {
                            {
                                xs,
                                x_data},
                            {
                                ys,
                                y_data}});
                    }
                }
                // predict today's price
                var y_pred_final = sess.run(prediction, feed_dict: new Dictionary<object, object> {
                    {
                        xs,
                        y_data}})[0][-1];
                // get sell prices and buy prices as trading signals
                this.sell_prices[symbol] = y_pred_final - np.std(y_data);
                this.buy_prices[symbol] = y_pred_final + np.std(y_data);
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
