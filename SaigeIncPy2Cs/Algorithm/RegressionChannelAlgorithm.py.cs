
using AddReference = clr.AddReference;

using np = numpy;

using timedelta = datetime.timedelta;

using datetime = datetime.datetime;

public static class RegressionChannelAlgorithm {
    
    static RegressionChannelAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class RegressionChannelAlgorithm
        : QCAlgorithm {
        
        public object _holdings;
        
        public object _rc;
        
        public object _spy;
        
        public virtual object Initialize() {
            this.SetCash(100000);
            this.SetStartDate(2009, 1, 1);
            this.SetEndDate(2015, 1, 1);
            var equity = this.AddEquity("SPY", Resolution.Minute);
            this._spy = equity.Symbol;
            this._holdings = equity.Holdings;
            this._rc = this.RC(this._spy, 30, 2, Resolution.Daily);
            var stockPlot = Chart("Trade Plot");
            stockPlot.AddSeries(Series("Buy", SeriesType.Scatter, 0));
            stockPlot.AddSeries(Series("Sell", SeriesType.Scatter, 0));
            stockPlot.AddSeries(Series("UpperChannel", SeriesType.Line, 0));
            stockPlot.AddSeries(Series("LowerChannel", SeriesType.Line, 0));
            stockPlot.AddSeries(Series("Regression", SeriesType.Line, 0));
            this.AddChart(stockPlot);
        }
        
        public virtual object OnData(object data) {
            if (!this._rc.IsReady || !data.ContainsKey(this._spy)) {
                return;
            }
            if (data[this._spy] == null) {
                return;
            }
            var value = data[this._spy].Value;
            if (this._holdings.Quantity <= 0 && value < this._rc.LowerChannel.Current.Value) {
                this.SetHoldings(this._spy, 1);
                this.Plot("Trade Plot", "Buy", value);
            }
            if (this._holdings.Quantity >= 0 && value > this._rc.UpperChannel.Current.Value) {
                this.SetHoldings(this._spy, -1);
                this.Plot("Trade Plot", "Sell", value);
            }
        }
        
        public virtual object OnEndOfDay() {
            this.Plot("Trade Plot", "UpperChannel", this._rc.UpperChannel.Current.Value);
            this.Plot("Trade Plot", "LowerChannel", this._rc.LowerChannel.Current.Value);
            this.Plot("Trade Plot", "Regression", this._rc.LinearRegression.Current.Value);
        }
    }
}
