
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using SubscriptionDataSource = QuantConnect.Data.SubscriptionDataSource;

using PythonData = QuantConnect.Python.PythonData;

using datetime = datetime.datetime;

using json;

public static class CustomDataRegressionAlgorithm {
    
    static CustomDataRegressionAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Algorithm");
    }
    
    public class CustomDataRegressionAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2011, 9, 13);
            this.SetEndDate(2015, 12, 1);
            this.SetCash(100000);
            var resolution = this.LiveMode ? Resolution.Second : Resolution.Daily;
            this.AddData(Bitcoin, "BTC", resolution);
        }
        
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                if (data["BTC"].Close != 0) {
                    this.Order("BTC", this.Portfolio.MarginRemaining / abs(data["BTC"].Close + 1));
                }
            }
        }
    }
    
    // Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data
    public class Bitcoin
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            if (isLiveMode) {
                return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest);
            }
            //return "http://my-ftp-server.com/futures-data-" + date.ToString("Ymd") + ".zip"
            // OR simply return a fixed small data file. Large files will slow down your backtest
            return SubscriptionDataSource("https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm", SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            var coin = new Bitcoin();
            coin.Symbol = config.Symbol;
            if (isLiveMode) {
                // Example Line Format:
                // {"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
                try {
                    var liveBTC = json.loads(line);
                    // If value is zero, return None
                    var value = liveBTC["last"];
                    if (value == 0) {
                        return null;
                    }
                    coin.Time = datetime.now();
                    coin.Value = value;
                    coin["Open"] = float(liveBTC["open"]);
                    coin["High"] = float(liveBTC["high"]);
                    coin["Low"] = float(liveBTC["low"]);
                    coin["Close"] = float(liveBTC["last"]);
                    coin["Ask"] = float(liveBTC["ask"]);
                    coin["Bid"] = float(liveBTC["bid"]);
                    coin["VolumeBTC"] = float(liveBTC["volume"]);
                    coin["WeightedPrice"] = float(liveBTC["vwap"]);
                    return coin;
                } catch (ValueError) {
                    // Do nothing, possible error in json decoding
                    return null;
                }
            }
            // Example Line Format:
            // Date      Open   High    Low     Close   Volume (BTC)    Volume (Currency)   Weighted Price
            // 2011-09-13 5.8    6.0     5.65    5.97    58.37138238,    346.0973893944      5.929230648356
            if (!(line.strip() && line[0].isdigit())) {
                return null;
            }
            try {
                var data = line.split(",");
                coin.Time = datetime.strptime(data[0], "%Y-%m-%d");
                coin.Value = float(data[4]);
                coin["Open"] = float(data[1]);
                coin["High"] = float(data[2]);
                coin["Low"] = float(data[3]);
                coin["Close"] = float(data[4]);
                coin["VolumeBTC"] = float(data[5]);
                coin["VolumeUSD"] = float(data[6]);
                coin["WeightedPrice"] = float(data[7]);
                return coin;
            } catch (ValueError) {
                // Do nothing, possible error in json decoding
                return null;
            }
        }
    }
}
