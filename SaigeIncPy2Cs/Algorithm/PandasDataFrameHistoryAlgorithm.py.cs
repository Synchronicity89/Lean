
using AddReference = clr.AddReference;

using PythonQuandl = QuantConnect.Python.PythonQuandl;

using EquityExchange = QuantConnect.Securities.Equity.EquityExchange;

using Universe = QuantConnect.Data.UniverseSelection.Universe;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class PandasDataFrameHistoryAlgorithm {
    
    static PandasDataFrameHistoryAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class PandasDataFrameHistoryAlgorithm
        : QCAlgorithm {
        
        public object eur;
        
        public object option;
        
        public object sp1;
        
        public object spy;
        
        public object spyDailySma;
        
        public virtual object Initialize() {
            this.SetStartDate(2014, 6, 9);
            this.SetEndDate(2014, 6, 9);
            this.spy = this.AddEquity("SPY", Resolution.Daily).Symbol;
            this.eur = this.AddForex("EURUSD", Resolution.Daily).Symbol;
            var aapl = this.AddEquity("AAPL", Resolution.Minute).Symbol;
            this.option = Symbol.CreateOption(aapl, Market.USA, OptionStyle.American, OptionRight.Call, 750, new datetime(2014, 10, 18));
            this.AddOptionContract(this.option);
            var sp1 = this.AddData(QuandlFuture, "CHRIS/CME_SP1", Resolution.Daily);
            sp1.Exchange = EquityExchange();
            this.sp1 = sp1.Symbol;
            this.AddUniverse(this.CoarseSelection);
        }
        
        public virtual object CoarseSelection(object coarse) {
            if (this.Portfolio.Invested) {
                return Universe.Unchanged;
            }
            var selected = (from x in coarse
                where new List<object> {
                    "AAA",
                    "AIG",
                    "BAC"
                }.Contains(x.Symbol.Value)
                select x.Symbol).ToList();
            if (selected.Count == 0) {
                return Universe.Unchanged;
            }
            var universeHistory = this.History(selected, 10, Resolution.Daily);
            foreach (var symbol in selected) {
                this.AssertHistoryIndex(universeHistory, "close", 10, "", symbol);
            }
            return selected;
        }
        
        public virtual object OnData(object data) {
            object index;
            if (this.Portfolio.Invested) {
                return;
            }
            // we can get history in initialize to set up indicators and such
            this.spyDailySma = SimpleMovingAverage(14);
            // get the last calendar year's worth of SPY data at the configured resolution (daily)
            var tradeBarHistory = this.History(new List<string> {
                "SPY"
            }, new timedelta(365));
            this.AssertHistoryIndex(tradeBarHistory, "close", 251, "SPY", this.spy);
            // get the last calendar year's worth of EURUSD data at the configured resolution (daily)
            var quoteBarHistory = this.History(new List<string> {
                "EURUSD"
            }, new timedelta(298));
            this.AssertHistoryIndex(quoteBarHistory, "bidclose", 251, "EURUSD", this.eur);
            var optionHistory = this.History(new List<object> {
                this.option
            }, new timedelta(3));
            optionHistory.index = optionHistory.index.droplevel(level: new List<int> {
                0,
                1,
                2
            });
            this.AssertHistoryIndex(optionHistory, "bidclose", 390, "", this.option);
            // get the last calendar year's worth of quandl data at the configured resolution (daily)
            var quandlHistory = this.History(QuandlFuture, "CHRIS/CME_SP1", new timedelta(365));
            this.AssertHistoryIndex(quandlHistory, "settle", 251, "CHRIS/CME_SP1", this.sp1);
            // we can loop over the return value from these functions and we get TradeBars
            // we can use these TradeBars to initialize indicators or perform other math
            this.spyDailySma.Reset();
            foreach (var _tup_1 in tradeBarHistory.loc["SPY"].iterrows()) {
                index = _tup_1.Item1;
                var tradeBar = _tup_1.Item2;
                this.spyDailySma.Update(index, tradeBar["close"]);
            }
            // we can loop over the return values from these functions and we'll get Quandl data
            // this can be used in much the same way as the tradeBarHistory above
            this.spyDailySma.Reset();
            foreach (var _tup_2 in quandlHistory.loc["CHRIS/CME_SP1"].iterrows()) {
                index = _tup_2.Item1;
                var quandl = _tup_2.Item2;
                this.spyDailySma.Update(index, quandl["settle"]);
            }
            this.SetHoldings(this.eur, 1);
        }
        
        public virtual object AssertHistoryIndex(
            object df,
            object column,
            object expected,
            object ticker,
            object symbol) {
            if (df.empty) {
                throw new Exception("Empty history data frame for {symbol}");
            }
            if (!df.Contains(column)) {
                throw new Exception("Could not unstack df. Columns: {', '.join(df.columns)} | {column}");
            }
            var value = df.iat[0,0];
            var df2 = df.xs(df.index.get_level_values("time")[0], level: "time");
            var df3 = df[column].unstack(level: 0);
            try {
                // str(Symbol.ID)
                this.AssertHistoryCount("df.iloc[0]", df.iloc[0], df.columns.Count);
                this.AssertHistoryCount("df.loc[str({symbol.ID})]", df.loc[symbol.ID.ToString()], expected);
                this.AssertHistoryCount("df.xs(str({symbol.ID}))", df.xs(symbol.ID.ToString()), expected);
                this.AssertHistoryCount("df.at[(str({symbol.ID}),), '{column}']", df.at[Tuple.Create(symbol.ID.ToString()),column].ToList(), expected);
                this.AssertHistoryCount("df2.loc[str({symbol.ID})]", df2.loc[symbol.ID.ToString()], df2.columns.Count);
                this.AssertHistoryCount("df3[str({symbol.ID})]", df3[symbol.ID.ToString()], expected);
                this.AssertHistoryCount("df3.get(str({symbol.ID}))", df3.get(symbol.ID.ToString()), expected);
                // str(Symbol)
                this.AssertHistoryCount("df.loc[str({symbol})]", df.loc[symbol.ToString()], expected);
                this.AssertHistoryCount("df.xs(str({symbol}))", df.xs(symbol.ToString()), expected);
                this.AssertHistoryCount("df.at[(str({symbol}),), '{column}']", df.at[Tuple.Create(symbol.ToString()),column].ToList(), expected);
                this.AssertHistoryCount("df2.loc[str({symbol})]", df2.loc[symbol.ToString()], df2.columns.Count);
                this.AssertHistoryCount("df3[str({symbol})]", df3[symbol.ToString()], expected);
                this.AssertHistoryCount("df3.get(str({symbol}))", df3.get(symbol.ToString()), expected);
                // str : Symbol.Value
                if (ticker.Count == 0) {
                    return;
                }
                this.AssertHistoryCount("df.loc[{ticker}]", df.loc[ticker], expected);
                this.AssertHistoryCount("df.xs({ticker})", df.xs(ticker), expected);
                this.AssertHistoryCount("df.at[(ticker,), '{column}']", df.at[Tuple.Create(ticker),column].ToList(), expected);
                this.AssertHistoryCount("df2.loc[{ticker}]", df2.loc[ticker], df2.columns.Count);
                this.AssertHistoryCount("df3[{ticker}]", df3[ticker], expected);
                this.AssertHistoryCount("df3.get({ticker})", df3.get(ticker), expected);
            } catch (Exception) {
                var symbols = new HashSet<object>(df.index.get_level_values(level: "symbol"));
                throw new Exception("{symbols}, {symbol.ID}, {symbol}, {ticker}. {e}");
            }
        }
        
        public virtual object AssertHistoryCount(object methodCall, object tradeBarHistory, object expected) {
            object count;
            if (tradeBarHistory is list) {
                count = tradeBarHistory.Count;
            } else {
                count = tradeBarHistory.index.Count;
            }
            if (count != expected) {
                throw new Exception("{methodCall} expected {expected}, but received {count}");
            }
        }
    }
    
    // Custom quandl data type for setting customized value column name. Value column is used for the primary trading calculations and charting.
    public class QuandlFuture
        : PythonQuandl {
        
        public string ValueColumnName;
        
        public QuandlFuture() {
            this.ValueColumnName = "Settle";
        }
    }
}
