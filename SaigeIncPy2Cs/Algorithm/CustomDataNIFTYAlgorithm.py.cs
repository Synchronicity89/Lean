
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

public static class CustomDataNIFTYAlgorithm {
    
    static CustomDataNIFTYAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomDataNIFTYAlgorithm
        : QCAlgorithm {
        
        public bool EnableAutomaticIndicatorWarmUp;
        
        public int minimumCorrelationHistory;
        
        public List<object> prices;
        
        public object today;
        
        public virtual object Initialize() {
            this.SetStartDate(2008, 1, 8);
            this.SetEndDate(2014, 7, 25);
            this.SetCash(100000);
            // Define the symbol and "type" of our generic data:
            var rupee = this.AddData(DollarRupee, "USDINR", Resolution.Daily).Symbol;
            var nifty = this.AddData(Nifty, "NIFTY", Resolution.Daily).Symbol;
            this.EnableAutomaticIndicatorWarmUp = true;
            var rupeeSma = this.SMA(rupee, 20);
            var niftySma = this.SMA(rupee, 20);
            this.Log("SMA - Is ready? USDINR: {rupeeSma.IsReady} NIFTY: {niftySma.IsReady}");
            this.minimumCorrelationHistory = 50;
            this.today = new CorrelationPair();
            this.prices = new List<object>();
        }
        
        public virtual object OnData(object data) {
            object code;
            if (data.ContainsKey("USDINR")) {
                this.today = new CorrelationPair(this.Time);
                this.today.CurrencyPrice = data["USDINR"].Close;
            }
            if (!data.ContainsKey("NIFTY")) {
                return;
            }
            this.today.NiftyPrice = data["NIFTY"].Close;
            if (this.today.date() == data["NIFTY"].Time.date()) {
                this.prices.append(this.today);
                if (this.prices.Count > this.minimumCorrelationHistory) {
                    this.prices.pop(0);
                }
            }
            // Strategy
            if (this.Time.weekday() != 2) {
                return;
            }
            var cur_qnty = this.Portfolio["NIFTY"].Quantity;
            var quantity = math.floor(this.Portfolio.MarginRemaining * 0.9) / data["NIFTY"].Close;
            var hi_nifty = max(from price in this.prices
                select price.NiftyPrice);
            var lo_nifty = min(from price in this.prices
                select price.NiftyPrice);
            if (data["NIFTY"].Open >= hi_nifty) {
                code = this.Order("NIFTY", quantity - cur_qnty);
                this.Debug("LONG  {0} Time: {1} Quantity: {2} Portfolio: {3} Nifty: {4} Buying Power: {5}".format(code, this.Time, quantity, this.Portfolio["NIFTY"].Quantity, data["NIFTY"].Close, this.Portfolio.TotalPortfolioValue));
            } else if (data["NIFTY"].Open <= lo_nifty) {
                code = this.Order("NIFTY", -quantity - cur_qnty);
                this.Debug("SHORT {0} Time: {1} Quantity: {2} Portfolio: {3} Nifty: {4} Buying Power: {5}".format(code, this.Time, quantity, this.Portfolio["NIFTY"].Quantity, data["NIFTY"].Close, this.Portfolio.TotalPortfolioValue));
            }
        }
    }
    
    // NIFTY Custom Data Class
    public class Nifty
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            return SubscriptionDataSource("https://www.dropbox.com/s/rsmg44jr6wexn2h/CNXNIFTY.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            if (!(line.strip() && line[0].isdigit())) {
                return null;
            }
            // New Nifty object
            var index = new Nifty();
            index.Symbol = config.Symbol;
            try {
                // Example File Format:
                // Date,       Open       High        Low       Close     Volume      Turnover
                // 2011-09-13  7792.9    7799.9     7722.65    7748.7    116534670    6107.78
                var data = line.split(",");
                index.Time = datetime.strptime(data[0], "%Y-%m-%d");
                index.Value = data[4];
                index["Open"] = float(data[1]);
                index["High"] = float(data[2]);
                index["Low"] = float(data[3]);
                index["Close"] = float(data[4]);
            } catch (ValueError) {
                // Do nothing
                return null;
            }
            return index;
        }
    }
    
    // Dollar Rupe is a custom data type we create for this algorithm
    public class DollarRupee
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            return SubscriptionDataSource("https://www.dropbox.com/s/m6ecmkg9aijwzy2/USDINR.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            if (!(line.strip() && line[0].isdigit())) {
                return null;
            }
            // New USDINR object
            var currency = new DollarRupee();
            currency.Symbol = config.Symbol;
            try {
                var data = line.split(",");
                currency.Time = datetime.strptime(data[0], "%Y-%m-%d");
                currency.Value = data[1];
                currency["Close"] = float(data[1]);
            } catch (ValueError) {
                // Do nothing
                return null;
            }
            return currency;
        }
    }
    
    // Correlation Pair is a helper class to combine two data points which we'll use to perform the correlation.
    public class CorrelationPair {
        
        public object _date;
        
        public int CurrencyPrice;
        
        public int NiftyPrice;
        
        public CorrelationPair(params object [] args) {
            this.NiftyPrice = 0;
            this.CurrencyPrice = 0;
            this._date = datetime.min;
            if (args.Count > 0) {
                this._date = args[0];
            }
        }
        
        public virtual object date() {
            return this._date.date();
        }
    }
}
