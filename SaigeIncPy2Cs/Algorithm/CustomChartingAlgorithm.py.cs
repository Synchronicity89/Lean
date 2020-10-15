
using AddReference = clr.AddReference;

using np = numpy;

using d = @decimal;

using timedelta = datetime.timedelta;

using datetime = datetime.datetime;

using System;

public static class CustomChartingAlgorithm {
    
    static CustomChartingAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomChartingAlgorithm
        : QCAlgorithm {
        
        public double fastMA;
        
        public object lastPrice;
        
        public int resample;
        
        public int resamplePeriod;
        
        public double slowMA;
        
        public virtual object Initialize() {
            this.SetStartDate(2016, 1, 1);
            this.SetEndDate(2017, 1, 1);
            this.SetCash(100000);
            this.AddEquity("SPY", Resolution.Daily);
            // In your initialize method:
            // Chart - Master Container for the Chart:
            var stockPlot = Chart("Trade Plot");
            // On the Trade Plotter Chart we want 3 series: trades and price:
            stockPlot.AddSeries(Series("Buy", SeriesType.Scatter, 0));
            stockPlot.AddSeries(Series("Sell", SeriesType.Scatter, 0));
            stockPlot.AddSeries(Series("Price", SeriesType.Line, 0));
            this.AddChart(stockPlot);
            // On the Average Cross Chart we want 2 series, slow MA and fast MA
            var avgCross = Chart("Average Cross");
            avgCross.AddSeries(Series("FastMA", SeriesType.Line, 0));
            avgCross.AddSeries(Series("SlowMA", SeriesType.Line, 0));
            this.AddChart(avgCross);
            this.fastMA = 0;
            this.slowMA = 0;
            this.lastPrice = 0;
            this.resample = datetime.min;
            this.resamplePeriod = (this.EndDate - this.StartDate) / 2000;
        }
        
        public virtual object OnData(object slice) {
            if (slice["SPY"] == null) {
                return;
            }
            this.lastPrice = slice["SPY"].Close;
            if (this.fastMA == 0) {
                this.fastMA = this.lastPrice;
            }
            if (this.slowMA == 0) {
                this.slowMA = this.lastPrice;
            }
            this.fastMA = 0.01 * this.lastPrice + 0.99 * this.fastMA;
            this.slowMA = 0.001 * this.lastPrice + 0.999 * this.slowMA;
            if (this.Time > this.resample) {
                this.resample = this.Time + this.resamplePeriod;
                this.Plot("Average Cross", "FastMA", this.fastMA);
                this.Plot("Average Cross", "SlowMA", this.slowMA);
            }
            // On the 5th days when not invested buy:
            if (!this.Portfolio.Invested && this.Time.day % 13 == 0) {
                this.Order("SPY", Convert.ToInt32(this.Portfolio.MarginRemaining / this.lastPrice));
                this.Plot("Trade Plot", "Buy", this.lastPrice);
            } else if (this.Time.day % 21 == 0 && this.Portfolio.Invested) {
                this.Plot("Trade Plot", "Sell", this.lastPrice);
                this.Liquidate();
            }
        }
        
        public virtual object OnEndOfDay() {
            //Log the end of day prices:
            this.Plot("Trade Plot", "Price", this.lastPrice);
        }
    }
}
