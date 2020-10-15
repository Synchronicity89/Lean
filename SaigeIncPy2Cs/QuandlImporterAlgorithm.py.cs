
using AddReference = clr.AddReference;

using PythonQuandl = QuantConnect.Python.PythonQuandl;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

public static class QuandlImporterAlgorithm {
    
    static QuandlImporterAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Indicators");
        AddReference("QuantConnect.Common");
    }
    
    public class QuandlImporterAlgorithm
        : QCAlgorithm {
        
        public string quandlCode;
        
        public object sma;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.quandlCode = "WIKI/IBM";
            //# Optional argument - personal token necessary for restricted dataset
            // Quandl.SetAuthCode("your-quandl-token")
            this.SetStartDate(2014, 4, 1);
            this.SetEndDate(datetime.today() - new timedelta(1));
            this.SetCash(25000);
            this.AddData(QuandlCustomColumns, this.quandlCode, Resolution.Daily, TimeZones.NewYork);
            this.sma = this.SMA(this.quandlCode, 14);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        public virtual object OnData(object data) {
            if (!this.Portfolio.HoldStock) {
                this.SetHoldings(this.quandlCode, 1);
                this.Debug("Purchased {0} >> {1}".format(this.quandlCode, this.Time));
            }
            this.Plot(this.quandlCode, "PriceSMA", this.sma.Current.Value);
        }
    }
    
    // Custom quandl data type for setting customized value column name. Value column is used for the primary trading calculations and charting.
    public class QuandlCustomColumns
        : PythonQuandl {
        
        public string ValueColumnName;
        
        public QuandlCustomColumns() {
            // Define ValueColumnName: cannot be None, Empty or non-existant column name
            this.ValueColumnName = "adj. close";
        }
    }
}
