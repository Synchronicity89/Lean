
using AddReference = clr.AddReference;

using PythonQuandl = QuantConnect.Python.PythonQuandl;

public static class CustomDataIndicatorExtensionsAlgorithm {
    
    static CustomDataIndicatorExtensionsAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Indicators");
    }
    
    public class CustomDataIndicatorExtensionsAlgorithm
        : QCAlgorithm {
        
        public object ratio;
        
        public string vix;
        
        public object vix_sma;
        
        public string vxv;
        
        public object vxv_sma;
        
        // Initialize the data and resolution you require for your strategy
        public virtual object Initialize() {
            this.SetStartDate(2014, 1, 1);
            this.SetEndDate(2018, 1, 1);
            this.SetCash(25000);
            this.vix = "CBOE/VIX";
            this.vxv = "CBOE/VXV";
            // Define the symbol and "type" of our generic data
            this.AddData(QuandlVix, this.vix, Resolution.Daily);
            this.AddData(Quandl, this.vxv, Resolution.Daily);
            // Set up default Indicators, these are just 'identities' of the closing price
            this.vix_sma = this.SMA(this.vix, 1, Resolution.Daily);
            this.vxv_sma = this.SMA(this.vxv, 1, Resolution.Daily);
            // This will create a new indicator whose value is smaVXV / smaVIX
            this.ratio = IndicatorExtensions.Over(this.vxv_sma, this.vix_sma);
            // Plot indicators each time they update using the PlotIndicator function
            this.PlotIndicator("Ratio", this.ratio);
            this.PlotIndicator("Data", this.vix_sma, this.vxv_sma);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            // Wait for all indicators to fully initialize
            if (!(this.vix_sma.IsReady && this.vxv_sma.IsReady && this.ratio.IsReady)) {
                return;
            }
            if (!this.Portfolio.Invested && this.ratio.Current.Value > 1) {
                this.MarketOrder(this.vix, 100);
            } else if (this.ratio.Current.Value < 1) {
                this.Liquidate();
            }
        }
    }
    
    public class QuandlVix
        : PythonQuandl {
        
        public string ValueColumnName;
        
        public QuandlVix() {
            this.ValueColumnName = "VIX Close";
        }
    }
}
