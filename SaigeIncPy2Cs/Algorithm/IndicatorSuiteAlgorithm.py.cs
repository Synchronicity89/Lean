
using AddReference = clr.AddReference;

using System.Collections.Generic;

using System;

public static class IndicatorSuiteAlgorithm {
    
    static IndicatorSuiteAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Indicators");
    }
    
    // Demonstration algorithm of popular indicators and plotting them.
    public class IndicatorSuiteAlgorithm
        : QCAlgorithm {
        
        public string customSymbol;
        
        public Dictionary<string, object> indicators;
        
        public object maxCustom;
        
        public object minCustom;
        
        public object price;
        
        public object ratio;
        
        public object rsiCustom;
        
        public Dictionary<string, object> selectorIndicators;
        
        public string symbol;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.symbol = "SPY";
            this.customSymbol = "WIKI/FB";
            this.price = null;
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2014, 12, 31);
            this.SetCash(25000);
            // Find more symbols here: http://quantconnect.com/data
            this.AddEquity(this.symbol, Resolution.Daily);
            this.AddData(Quandl, this.customSymbol, Resolution.Daily);
            // Set up default Indicators, these indicators are defined on the Value property of incoming data (except ATR and AROON which use the full TradeBar object)
            this.indicators = new Dictionary<object, object> {
                {
                    "BB",
                    this.BB(this.symbol, 20, 1, MovingAverageType.Simple, Resolution.Daily)},
                {
                    "RSI",
                    this.RSI(this.symbol, 14, MovingAverageType.Simple, Resolution.Daily)},
                {
                    "EMA",
                    this.EMA(this.symbol, 14, Resolution.Daily)},
                {
                    "SMA",
                    this.SMA(this.symbol, 14, Resolution.Daily)},
                {
                    "MACD",
                    this.MACD(this.symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily)},
                {
                    "MOM",
                    this.MOM(this.symbol, 20, Resolution.Daily)},
                {
                    "MOMP",
                    this.MOMP(this.symbol, 20, Resolution.Daily)},
                {
                    "STD",
                    this.STD(this.symbol, 20, Resolution.Daily)},
                {
                    "MIN",
                    this.MIN(this.symbol, 14, Resolution.Daily)},
                {
                    "MAX",
                    this.MAX(this.symbol, 14, Resolution.Daily)},
                {
                    "ATR",
                    this.ATR(this.symbol, 14, MovingAverageType.Simple, Resolution.Daily)},
                {
                    "AROON",
                    this.AROON(this.symbol, 20, Resolution.Daily)}};
            //  Here we're going to define indicators using 'selector' functions. These 'selector' functions will define what data gets sent into the indicator
            //  These functions have a signature like the following: decimal Selector(BaseData baseData), and can be defined like: baseData => baseData.Value
            //  We'll define these 'selector' functions to select the Low value
            //
            //  For more information on 'anonymous functions' see: http:#en.wikipedia.org/wiki/Anonymous_function
            //                                                     https:#msdn.microsoft.com/en-us/library/bb397687.aspx
            //
            this.selectorIndicators = new Dictionary<object, object> {
                {
                    "BB",
                    this.BB(this.symbol, 20, 1, MovingAverageType.Simple, Resolution.Daily, Field.Low)},
                {
                    "RSI",
                    this.RSI(this.symbol, 14, MovingAverageType.Simple, Resolution.Daily, Field.Low)},
                {
                    "EMA",
                    this.EMA(this.symbol, 14, Resolution.Daily, Field.Low)},
                {
                    "SMA",
                    this.SMA(this.symbol, 14, Resolution.Daily, Field.Low)},
                {
                    "MACD",
                    this.MACD(this.symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily, Field.Low)},
                {
                    "MOM",
                    this.MOM(this.symbol, 20, Resolution.Daily, Field.Low)},
                {
                    "MOMP",
                    this.MOMP(this.symbol, 20, Resolution.Daily, Field.Low)},
                {
                    "STD",
                    this.STD(this.symbol, 20, Resolution.Daily, Field.Low)},
                {
                    "MIN",
                    this.MIN(this.symbol, 14, Resolution.Daily, Field.High)},
                {
                    "MAX",
                    this.MAX(this.symbol, 14, Resolution.Daily, Field.Low)},
                {
                    "ATR",
                    this.ATR(this.symbol, 14, MovingAverageType.Simple, Resolution.Daily, Func[IBaseData,IBaseDataBar](this.selector_double_TradeBar))},
                {
                    "AROON",
                    this.AROON(this.symbol, 20, Resolution.Daily, Func[IBaseData,IBaseDataBar](this.selector_double_TradeBar))}};
            // Custom Data Indicator:
            this.rsiCustom = this.RSI(this.customSymbol, 14, MovingAverageType.Simple, Resolution.Daily);
            this.minCustom = this.MIN(this.customSymbol, 14, Resolution.Daily);
            this.maxCustom = this.MAX(this.customSymbol, 14, Resolution.Daily);
            // in addition to defining indicators on a single security, you can all define 'composite' indicators.
            // these are indicators that require multiple inputs. the most common of which is a ratio.
            // suppose we seek the ratio of BTC to SPY, we could write the following:
            var spyClose = Identity(this.symbol);
            var fbClose = Identity(this.customSymbol);
            // this will create a new indicator whose value is FB/SPY
            this.ratio = IndicatorExtensions.Over(fbClose, spyClose);
            // we can also easily plot our indicators each time they update using th PlotIndicator function
            this.PlotIndicator("Ratio", this.ratio);
            // The following methods will add multiple charts to the algorithm output.
            // Those chatrs names will be used later to plot different series in a particular chart.
            // For more information on Lean Charting see: https://www.quantconnect.com/docs#Charting
            Chart("BB");
            Chart("STD");
            Chart("ATR");
            Chart("AROON");
            Chart("MACD");
            Chart("Averages");
            // Here we make use of the Schelude method to update the plots once per day at market close.
            this.Schedule.On(this.DateRules.EveryDay(), this.TimeRules.BeforeMarketClose(this.symbol), this.update_plots);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.indicators["BB"].IsReady || !this.indicators["RSI"].IsReady) {
                return;
            }
            this.price = data[this.symbol].Close;
            if (!this.Portfolio.HoldStock) {
                var quantity = Convert.ToInt32(this.Portfolio.Cash / this.price);
                this.Order(this.symbol, quantity);
                this.Debug("Purchased SPY on " + this.Time.strftime("%Y-%m-%d"));
            }
        }
        
        public virtual object update_plots() {
            if (!this.indicators["BB"].IsReady || !this.indicators["STD"].IsReady) {
                return;
            }
            // Plots can also be created just with this one line command.
            this.Plot("RSI", this.indicators["RSI"]);
            // Custom data indicator
            this.Plot("RSI-FB", this.rsiCustom);
            // Here we make use of the chats decalred in the Initialize method, plotting multiple series
            // in each chart.
            this.Plot("STD", "STD", this.indicators["STD"].Current.Value);
            this.Plot("BB", "Price", this.price);
            this.Plot("BB", "BollingerUpperBand", this.indicators["BB"].UpperBand.Current.Value);
            this.Plot("BB", "BollingerMiddleBand", this.indicators["BB"].MiddleBand.Current.Value);
            this.Plot("BB", "BollingerLowerBand", this.indicators["BB"].LowerBand.Current.Value);
            this.Plot("AROON", "Aroon", this.indicators["AROON"].Current.Value);
            this.Plot("AROON", "AroonUp", this.indicators["AROON"].AroonUp.Current.Value);
            this.Plot("AROON", "AroonDown", this.indicators["AROON"].AroonDown.Current.Value);
            // The following Plot method calls are commented out because of the 10 series limit for backtests
            //self.Plot('ATR', 'ATR', self.indicators['ATR'].Current.Value)
            //self.Plot('ATR', 'ATRDoubleBar', self.selectorIndicators['ATR'].Current.Value)
            //self.Plot('Averages', 'SMA', self.indicators['SMA'].Current.Value)
            //self.Plot('Averages', 'EMA', self.indicators['EMA'].Current.Value)
            //self.Plot('MOM', self.indicators['MOM'].Current.Value)
            //self.Plot('MOMP', self.indicators['MOMP'].Current.Value)
            //self.Plot('MACD', 'MACD', self.indicators['MACD'].Current.Value)
            //self.Plot('MACD', 'MACDSignal', self.indicators['MACD'].Signal.Current.Value)
        }
        
        public virtual object selector_double_TradeBar(object bar) {
            var trade_bar = TradeBar();
            trade_bar.Close = 2 * bar.Close;
            trade_bar.DataType = bar.DataType;
            trade_bar.High = 2 * bar.High;
            trade_bar.Low = 2 * bar.Low;
            trade_bar.Open = 2 * bar.Open;
            trade_bar.Symbol = bar.Symbol;
            trade_bar.Time = bar.Time;
            trade_bar.Value = 2 * bar.Value;
            trade_bar.Period = bar.Period;
            return trade_bar;
        }
    }
}
