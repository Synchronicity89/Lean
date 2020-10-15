
using AddReference = clr.AddReference;

using USEnergyAPI = QuantConnect.Data.Custom.USEnergy.USEnergyAPI;

public static class USEnergyInformationAdministrationAlgorithm {
    
    static USEnergyInformationAdministrationAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class USEnergyInformationAdministrationAlgorithm
        : QCAlgorithm {
        
        public object emaFast;
        
        public object emaSlow;
        
        public object energySymbol;
        
        public string energyTicker;
        
        public object tiingoSymbol;
        
        public string tiingoTicker;
        
        public virtual object Initialize() {
            // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
            this.SetStartDate(2017, 1, 1);
            this.SetEndDate(2017, 12, 31);
            this.SetCash(100000);
            // Set your Tiingo API Token here
            Tiingo.SetAuthCode("my-tiingo-api-token");
            // Set your US Energy Information Administration (EIA) API Token here
            USEnergyAPI.SetAuthCode("my-us-energy-information-api-token");
            this.tiingoTicker = "AAPL";
            this.energyTicker = "NUC_STATUS.OUT.US.D";
            this.tiingoSymbol = this.AddData(TiingoDailyData, this.tiingoTicker, Resolution.Daily).Symbol;
            this.energySymbol = this.AddData(USEnergyAPI, this.energyTicker, Resolution.Hour).Symbol;
            this.emaFast = this.EMA(this.tiingoSymbol, 5);
            this.emaSlow = this.EMA(this.tiingoSymbol, 10);
        }
        
        public virtual object OnData(object slice) {
            // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            if (!slice.ContainsKey(this.tiingoTicker) || !slice.ContainsKey(this.energyTicker)) {
                return;
            }
            // Extract Tiingo data from the slice
            var tiingoRow = slice[this.tiingoTicker];
            var energyRow = slice[this.energyTicker];
            this.Log("{self.Time} - {tiingoRow.Symbol.Value} - {tiingoRow.Close} {tiingoRow.Value} {tiingoRow.Price} - EmaFast:{self.emaFast} - EmaSlow:{self.emaSlow}");
            this.Log("{self.Time} - {energyRow.Symbol.Value} - {energyRow.Value}");
            // Simple EMA cross
            if (!this.Portfolio.Invested && this.emaFast > this.emaSlow) {
                this.SetHoldings(this.tiingoSymbol, 1);
            } else if (this.Portfolio.Invested && this.emaFast < this.emaSlow) {
                this.Liquidate(this.tiingoSymbol);
            }
        }
    }
}
