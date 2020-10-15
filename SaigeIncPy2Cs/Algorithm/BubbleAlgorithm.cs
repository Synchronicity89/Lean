
using AddReference = clr.AddReference;

using SubscriptionDataSource = QuantConnect.Data.SubscriptionDataSource;

using PythonData = QuantConnect.Python.PythonData;

using date = datetime.date;

using timedelta = datetime.timedelta;

using datetime = datetime.datetime;

using np = numpy;

using math;

using json;

using System.Collections.Generic;

public static class BubbleAlgorithm {
    
    static BubbleAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class BubbleAlgorithm
        : QCAlgorithm {
        
        public object _cCopy;
        
        public int _counter;
        
        public int _counter2;
        
        public object _currCape;
        
        public object _macd;
        
        public bool _newLow;
        
        public object _rsi;
        
        public List<object> _symbols;
        
        public virtual object Initialize() {
            this.SetCash(100000);
            this.SetStartDate(1998, 1, 1);
            this.SetEndDate(2014, 6, 1);
            this._symbols = new List<object>();
            this._macdDic = new Dictionary<object, object> {
            };
            this._rsiDic = new Dictionary<object, object> {
            };
            this._newLow = null;
            this._currCape = null;
            this._counter = 0;
            this._counter2 = 0;
            this._c = np.empty(new List<int> {
                4
            });
            this._cCopy = np.empty(new List<int> {
                4
            });
            this._symbols.append("SPY");
            // add CAPE data
            this.AddData(Cape, "CAPE");
            // # Present Social Media Stocks:
            // self._symbols.append("FB"), self._symbols.append("LNKD"),self._symbols.append("GRPN"), self._symbols.append("TWTR")
            // self.SetStartDate(2011, 1, 1)
            // self.SetEndDate(2014, 12, 1)
            // # 2008 Financials
            // self._symbols.append("C"), self._symbols.append("AIG"), self._symbols.append("BAC"), self._symbols.append("HBOS")
            // self.SetStartDate(2003, 1, 1)
            // self.SetEndDate(2011, 1, 1)
            // # 2000 Dot.com
            // self._symbols.append("IPET"), self._symbols.append("WBVN"), self._symbols.append("GCTY")
            // self.SetStartDate(1998, 1, 1)
            // self.SetEndDate(2000, 1, 1)
            foreach (var stock in this._symbols) {
                this.AddSecurity(SecurityType.Equity, stock, Resolution.Minute);
                this._macd = this.MACD(stock, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily);
                this._macdDic[stock] = this._macd;
                this._rsi = this.RSI(stock, 14, MovingAverageType.Exponential, Resolution.Daily);
                this._rsiDic[stock] = this._rsi;
            }
        }
        
        // Trying to find if current Cape is the lowest Cape in three months to indicate selling period
        public virtual object OnData(object data) {
            if (this._currCape && this._newLow != null) {
                try {
                    // Bubble territory
                    if (this._currCape > 20 && this._newLow == false) {
                        foreach (var stock in this._symbols) {
                            // Order stock based on MACD
                            // During market hours, stock is trading, and sufficient cash
                            if (this.Securities[stock].Holdings.Quantity == 0 && this._rsiDic[stock].Current.Value < 70 && this.Securities[stock].Price != 0 && this.Portfolio.Cash > this.Securities[stock].Price * 100 && this.Time.hour == 9 && this.Time.minute == 31) {
                                this.BuyStock(stock);
                                // Utilize RSI for overbought territories and liquidate that stock
                            }
                            if (this._rsiDic[stock].Current.Value > 70 && this.Securities[stock].Holdings.Quantity > 0 && this.Time.hour == 9 && this.Time.minute == 31) {
                                this.SellStock(stock);
                            }
                        }
                    } else if (this._newLow) {
                        // Undervalued territory            
                        foreach (var stock in this._symbols) {
                            // Sell stock based on MACD
                            if (this.Securities[stock].Holdings.Quantity > 0 && this._rsiDic[stock].Current.Value > 30 && this.Time.hour == 9 && this.Time.minute == 31) {
                                this.SellStock(stock);
                            } else if (this.Securities[stock].Holdings.Quantity == 0 && this._rsiDic[stock].Current.Value < 30 && Securities[stock].Price != 0 && this.Portfolio.Cash > this.Securities[stock].Price * 100 && this.Time.hour == 9 && this.Time.minute == 31) {
                                // Utilize RSI and MACD to understand oversold territories
                                this.BuyStock(stock);
                            }
                        }
                    } else if (this._currCape == 0) {
                        // Cape Ratio is missing from orignial data
                        // Most recent cape data is most likely to be missing 
                        this.Debug("Exiting due to no CAPE!");
                        this.Quit("CAPE ratio not supplied in data, exiting.");
                    }
                } catch {
                    // Do nothing
                    return null;
                }
            }
            if (!data.ContainsKey("CAPE")) {
                return;
            }
            this._newLow = false;
            // Adds first four Cape Ratios to array c
            this._currCape = data["CAPE"].Cape;
            if (this._counter < 4) {
                this._c[this._counter] = this._currCape;
                this._counter += 1;
            } else {
                // Replaces oldest Cape with current Cape
                // Checks to see if current Cape is lowest in the previous quarter
                // Indicating a sell off
                this._cCopy = this._c;
                this._cCopy = np.sort(this._cCopy);
                if (this._cCopy[0] > this._currCape) {
                    this._newLow = true;
                }
                this._c[this._counter2] = this._currCape;
                this._counter2 += 1;
                if (this._counter2 == 4) {
                    this._counter2 = 0;
                }
            }
            this.Debug("Current Cape: " + this._currCape.ToString() + " on " + this.Time.ToString());
            if (this._newLow) {
                this.Debug("New Low has been hit on " + this.Time.ToString());
            }
        }
        
        // Buy this symbol
        public virtual object BuyStock(object symbol) {
            var s = this.Securities[symbol].Holdings;
            if (this._macdDic[symbol].Current.Value > 0) {
                this.SetHoldings(symbol, 1);
                this.Debug("Purchasing: " + symbol.ToString() + "   MACD: " + this._macdDic[symbol].ToString() + "   RSI: " + this._rsiDic[symbol].ToString() + "   Price: " + round(this.Securities[symbol].Price, 2).ToString() + "   Quantity: " + s.Quantity.ToString());
            }
        }
        
        // Sell this symbol
        public virtual object SellStock(object symbol) {
            var s = this.Securities[symbol].Holdings;
            if (s.Quantity > 0 && this._macdDic[symbol].Current.Value < 0) {
                this.Liquidate(symbol);
                this.Debug("Selling: " + symbol.ToString() + " at sell MACD: " + this._macdDic[symbol].ToString() + "   RSI: " + this._rsiDic[symbol].ToString() + "   Price: " + round(this.Securities[symbol].Price, 2).ToString() + "   Profit from sale: " + s.LastTradeProfit.ToString());
            }
        }
    }
    
    //  Reader Method : using set of arguements we specify read out type. Enumerate until 
    //         the end of the data stream or file. E.g. Read CSV file line by line and convert into data types. 
    public class Cape
        : PythonData {
        
        // Return the URL string source of the file. This will be converted to a stream
        // <param name="config">Configuration object</param>
        // <param name="date">Date of this source file</param>
        // <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        // <returns>String URL of source file.</returns>
        public virtual object GetSource(object config, object date, object isLiveMode) {
            // Remember to add the "?dl=1" for dropbox links
            return SubscriptionDataSource("https://www.dropbox.com/s/ggt6blmib54q36e/CAPE.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
        }
        
        // <returns>BaseData type set by Subscription Method.</returns>
        // <param name="config">Config.</param>
        // <param name="line">Line.</param>
        // <param name="date">Date.</param>
        // <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            if (!(line.strip() && line[0].isdigit())) {
                return null;
            }
            // New Nifty object
            var index = new Cape();
            index.Symbol = config.Symbol;
            try {
                // Example File Format:
                // Date   |  Price |  Div  | Earning | CPI  | FractionalDate | Interest Rate | RealPrice | RealDiv | RealEarnings | CAPE
                // 2014.06  1947.09  37.38   103.12   238.343    2014.37          2.6           1923.95     36.94        101.89     25.55
                var data = line.split(",");
                // Dates must be in the format YYYY-MM-DD. If your data source does not have this format, you must use
                // DateTime.ParseExact() and explicit declare the format your data source has.
                index.Time = datetime.strptime(data[0], "%Y-%m");
                index["Cape"] = float(data[10]);
                index.Value = data[10];
            } catch (ValueError) {
                // Do nothing
                return null;
            }
            return index;
        }
    }
}
