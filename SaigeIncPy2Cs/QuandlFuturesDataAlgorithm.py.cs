
using AddReference = clr.AddReference;

using PythonQuandl = QuantConnect.Python.PythonQuandl;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

public static class QuandlFuturesDataAlgorithm {
    
    static QuandlFuturesDataAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class QuandlFuturesDataAlgorithm
        : QCAlgorithm {
        
        public string crude;
        
        //  Initialize the data and resolution you require for your strategy 
        public virtual object Initialize() {
            this.SetStartDate(2000, 1, 1);
            this.SetEndDate(datetime.now().date() - new timedelta(1));
            this.SetCash(25000);
            // Symbol corresponding to the quandl code
            this.crude = "SCF/CME_CL1_ON";
            this.AddData(QuandlFuture, this.crude, Resolution.Daily);
        }
        
        // Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
        public virtual object OnData(object data) {
            if (this.Portfolio.HoldStock) {
                return;
            }
            this.SetHoldings(this.crude, 1);
            this.Debug(this.Time.ToString() + " Purchased Crude Oil: ".ToString() + this.crude);
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
