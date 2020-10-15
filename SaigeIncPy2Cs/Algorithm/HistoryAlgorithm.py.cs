
using AddReference = clr.AddReference;

using PythonQuandl = QuantConnect.Python.PythonQuandl;

using EquityExchange = QuantConnect.Securities.Equity.EquityExchange;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class HistoryAlgorithm {
    
    static HistoryAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class HistoryAlgorithm
        : QCAlgorithm {
        
        public object spyDailySma;
        
        public virtual object Initialize() {
            object index;
            this.SetStartDate(2013, 10, 8);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity("SPY", Resolution.Daily);
            this.AddData(QuandlFuture, "CHRIS/CME_SP1", Resolution.Daily);
            // specifying the exchange will allow the history methods that accept a number of bars to return to work properly
            this.Securities["CHRIS/CME_SP1"].Exchange = EquityExchange();
            // we can get history in initialize to set up indicators and such
            this.spyDailySma = SimpleMovingAverage(14);
            // get the last calendar year's worth of SPY data at the configured resolution (daily)
            var tradeBarHistory = this.History(new List<object> {
                this.Securities["SPY"].Symbol
            }, new timedelta(365));
            this.AssertHistoryCount("History<TradeBar>([\"SPY\"], timedelta(365))", tradeBarHistory, 250);
            // get the last calendar day's worth of SPY data at the specified resolution
            tradeBarHistory = this.History(new List<string> {
                "SPY"
            }, new timedelta(1), Resolution.Minute);
            this.AssertHistoryCount("History([\"SPY\"], timedelta(1), Resolution.Minute)", tradeBarHistory, 390);
            // get the last 14 bars of SPY at the configured resolution (daily)
            tradeBarHistory = this.History(new List<string> {
                "SPY"
            }, 14);
            this.AssertHistoryCount("History([\"SPY\"], 14)", tradeBarHistory, 14);
            // get the last 14 minute bars of SPY
            tradeBarHistory = this.History(new List<string> {
                "SPY"
            }, 14, Resolution.Minute);
            this.AssertHistoryCount("History([\"SPY\"], 14, Resolution.Minute)", tradeBarHistory, 14);
            // we can loop over the return value from these functions and we get TradeBars
            // we can use these TradeBars to initialize indicators or perform other math
            foreach (var _tup_1 in tradeBarHistory.loc["SPY"].iterrows()) {
                index = _tup_1.Item1;
                var tradeBar = _tup_1.Item2;
                this.spyDailySma.Update(index, tradeBar["close"]);
            }
            // get the last calendar year's worth of quandl data at the configured resolution (daily)
            var quandlHistory = this.History(QuandlFuture, "CHRIS/CME_SP1", new timedelta(365));
            this.AssertHistoryCount("History(QuandlFuture, \"CHRIS/CME_SP1\", timedelta(365))", quandlHistory, 250);
            // get the last 14 bars of SPY at the configured resolution (daily)
            quandlHistory = this.History(QuandlFuture, "CHRIS/CME_SP1", 14);
            this.AssertHistoryCount("History(QuandlFuture, \"CHRIS/CME_SP1\", 14)", quandlHistory, 14);
            // we can loop over the return values from these functions and we'll get Quandl data
            // this can be used in much the same way as the tradeBarHistory above
            this.spyDailySma.Reset();
            foreach (var _tup_2 in quandlHistory.loc["CHRIS/CME_SP1"].iterrows()) {
                index = _tup_2.Item1;
                var quandl = _tup_2.Item2;
                this.spyDailySma.Update(index, quandl["settle"]);
            }
            // get the last year's worth of all configured Quandl data at the configured resolution (daily)
            //allQuandlData = self.History(QuandlFuture, timedelta(365))
            //self.AssertHistoryCount("History(QuandlFuture, timedelta(365))", allQuandlData, 250)
            // get the last 14 bars worth of Quandl data for the specified symbols at the configured resolution (daily)
            var allQuandlData = this.History(QuandlFuture, this.Securities.Keys, 14);
            this.AssertHistoryCount("History(QuandlFuture, self.Securities.Keys, 14)", allQuandlData, 14);
            // NOTE: using different resolutions require that they are properly implemented in your data type, since
            //  Quandl doesn't support minute data, this won't actually work, but if your custom data source has
            //  different resolutions, it would need to be implemented in the GetSource and Reader methods properly
            //quandlHistory = self.History(QuandlFuture, "CHRIS/CME_SP1", timedelta(7), Resolution.Minute)
            //quandlHistory = self.History(QuandlFuture, "CHRIS/CME_SP1", 14, Resolution.Minute)
            //allQuandlData = self.History(QuandlFuture, timedelta(365), Resolution.Minute)
            //allQuandlData = self.History(QuandlFuture, self.Securities.Keys, 14, Resolution.Minute)
            //allQuandlData = self.History(QuandlFuture, self.Securities.Keys, timedelta(1), Resolution.Minute)
            //allQuandlData = self.History(QuandlFuture, self.Securities.Keys, 14, Resolution.Minute)
            // get the last calendar year's worth of all quandl data
            allQuandlData = this.History(QuandlFuture, this.Securities.Keys, new timedelta(365));
            this.AssertHistoryCount("History(QuandlFuture, self.Securities.Keys, timedelta(365))", allQuandlData, 250);
            // we can also access the return value from the multiple symbol functions to request a single
            // symbol and then loop over it
            var singleSymbolQuandl = allQuandlData.loc["CHRIS/CME_SP1"];
            this.AssertHistoryCount("allQuandlData.loc[\"CHRIS/CME_SP1\"]", singleSymbolQuandl, 250);
            foreach (var quandl in singleSymbolQuandl) {
                // do something with 'CHRIS/CME_SP1.QuandlFuture' quandl data
            }
            var quandlSpyLows = allQuandlData.loc["CHRIS/CME_SP1"]["low"];
            this.AssertHistoryCount("allQuandlData.loc[\"CHRIS/CME_SP1\"][\"low\"]", quandlSpyLows, 250);
            foreach (var low in quandlSpyLows) {
                // do something with 'CHRIS/CME_SP1.QuandlFuture' quandl data
            }
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 1);
            }
        }
        
        public virtual object AssertHistoryCount(object methodCall, object tradeBarHistory, object expected) {
            var count = tradeBarHistory.index.Count;
            if (count != expected) {
                throw new Exception("{} expected {}, but received {}".format(methodCall, expected, count));
            }
        }
    }
    
    // Custom quandl data type for setting customized value column name. Value column is used for the primary trading calculations and charting.
    public class QuandlFuture
        : PythonQuandl {
        
        public string ValueColumnName;
        
        public QuandlFuture() {
            // Define ValueColumnName: cannot be None, Empty or non-existant column name
            // If ValueColumnName is "Close", do not use PythonQuandl, use Quandl:
            // self.AddData[QuandlFuture](self.crude, Resolution.Daily)
            this.ValueColumnName = "Settle";
        }
    }
}
