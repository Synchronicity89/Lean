
using OrderStatus = QuantConnect.Orders.OrderStatus;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using np = numpy;

using timedelta = datetime.timedelta;

using datetime = datetime.datetime;

using System.Collections.Generic;

public static class MultipleSymbolConsolidationAlgorithm {
    
    public class MultipleSymbolConsolidationAlgorithm
        : QCAlgorithm {
        
        public Dictionary<object, object> Data;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // This is the period of bars we'll be creating
            var BarPeriod = TimeSpan.FromMinutes(10);
            // This is the period of our sma indicators
            var SimpleMovingAveragePeriod = 10;
            // This is the number of consolidated bars we'll hold in symbol data for reference
            var RollingWindowSize = 10;
            // Holds all of our data keyed by each symbol
            this.Data = new Dictionary<object, object> {
            };
            // Contains all of our equity symbols
            var EquitySymbols = new List<string> {
                "AAPL",
                "SPY",
                "IBM"
            };
            // Contains all of our forex symbols
            var ForexSymbols = new List<string> {
                "EURUSD",
                "USDJPY",
                "EURGBP",
                "EURCHF",
                "USDCAD",
                "USDCHF",
                "AUDUSD",
                "NZDUSD"
            };
            this.SetStartDate(2014, 12, 1);
            this.SetEndDate(2015, 2, 1);
            // initialize our equity data
            foreach (var symbol in EquitySymbols) {
                var equity = this.AddEquity(symbol);
                this.Data[symbol] = new SymbolData(equity.Symbol, BarPeriod, RollingWindowSize);
            }
            // initialize our forex data 
            foreach (var symbol in ForexSymbols) {
                var forex = this.AddForex(symbol);
                this.Data[symbol] = new SymbolData(forex.Symbol, BarPeriod, RollingWindowSize);
            }
            // loop through all our symbols and request data subscriptions and initialize indicator
            foreach (var _tup_1 in this.Data.items()) {
                var symbol = _tup_1.Item1;
                var symbolData = _tup_1.Item2;
                // define the indicator
                symbolData.SMA = SimpleMovingAverage(this.CreateIndicatorName(symbol, "SMA" + SimpleMovingAveragePeriod.ToString(), Resolution.Minute), SimpleMovingAveragePeriod);
                // define a consolidator to consolidate data for this symbol on the requested period
                var consolidator = symbolData.Symbol.SecurityType == SecurityType.Equity ? TradeBarConsolidator(BarPeriod) : QuoteBarConsolidator(BarPeriod);
                // write up our consolidator to update the indicator
                consolidator.DataConsolidated += this.OnDataConsolidated;
                // we need to add this consolidator so it gets auto updates
                this.SubscriptionManager.AddConsolidator(symbolData.Symbol, consolidator);
            }
        }
        
        public virtual object OnDataConsolidated(object sender, object bar) {
            this.Data[bar.Symbol.Value].SMA.Update(bar.Time, bar.Close);
            this.Data[bar.Symbol.Value].Bars.Add(bar);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // Argument "data": Slice object, dictionary object with your stock data 
        public virtual object OnData(object data) {
            // loop through each symbol in our structure
            foreach (var symbol in this.Data.keys()) {
                var symbolData = this.Data[symbol];
                // this check proves that this symbol was JUST updated prior to this OnData function being called
                if (symbolData.IsReady() && symbolData.WasJustUpdated(this.Time)) {
                    if (!this.Portfolio[symbol].Invested) {
                        this.MarketOrder(symbol, 1);
                    }
                }
            }
        }
        
        // End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        // Method is called 10 minutes before closing to allow user to close out position.
        public virtual object OnEndOfDay() {
            var i = 0;
            foreach (var symbol in this.Data.keys().OrderBy(_p_1 => _p_1).ToList()) {
                var symbolData = this.Data[symbol];
                // we have too many symbols to plot them all, so plot every other
                i += 1;
                if (symbolData.IsReady() && i % 2 == 0) {
                    this.Plot(symbol, symbol, symbolData.SMA.Current.Value);
                }
            }
        }
    }
    
    public class SymbolData
        : object {
        
        public object BarPeriod;
        
        public object Bars;
        
        public None SMA;
        
        public object Symbol;
        
        public SymbolData(object symbol, object barPeriod, object windowSize) {
            this.Symbol = symbol;
            // The period used when population the Bars rolling window
            this.BarPeriod = barPeriod;
            // A rolling window of data, data needs to be pumped into Bars by using Bars.Update( tradeBar ) and can be accessed like:
            // mySymbolData.Bars[0] - most first recent piece of data
            // mySymbolData.Bars[5] - the sixth most recent piece of data (zero based indexing)
            this.Bars = RollingWindow[IBaseDataBar](windowSize);
            // The simple moving average indicator for our symbol
            this.SMA = null;
        }
        
        // Returns true if all the data in this instance is ready (indicators, rolling windows, ect...)
        public virtual object IsReady() {
            return this.Bars.IsReady && this.SMA.IsReady;
        }
        
        // Returns true if the most recent trade bar time matches the current time minus the bar's period, this
        // indicates that update was just called on this instance
        public virtual object WasJustUpdated(object current) {
            return this.Bars.Count > 0 && this.Bars[0].Time == current - this.BarPeriod;
        }
    }
}
