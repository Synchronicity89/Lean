
using AddReference = clr.AddReference;

using sign = numpy.sign;

using timedelta = datetime.timedelta;

public static class BasicTemplateIntrinioEconomicData {
    
    static BasicTemplateIntrinioEconomicData() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
        AddReference("QuantConnect.Indicators");
    }
    
    public class BasicTemplateIntrinioEconomicData
        : QCAlgorithm {
        
        public object bno;
        
        public object emaWti;
        
        public object uso;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2010, 1, 1);
            this.SetEndDate(2013, 12, 31);
            this.SetCash(100000);
            // Set your Intrinino user and password.
            IntrinioConfig.SetUserAndPassword("intrinio-username", "intrinio-password");
            // The Intrinio user and password can be also defined in the config.json file for local backtest.
            // Set Intrinio config to make 1 call each minute, default is 1 call each 5 seconds.
            //(1 call each minute is the free account limit for historical_data endpoint)
            IntrinioConfig.SetTimeIntervalBetweenCalls(new timedelta(minutes: 1));
            // United States Oil Fund LP
            this.uso = this.AddEquity("USO", Resolution.Daily).Symbol;
            this.Securities[this.uso].SetLeverage(2);
            // United States Brent Oil Fund LP
            this.bno = this.AddEquity("BNO", Resolution.Daily).Symbol;
            this.Securities[this.bno].SetLeverage(2);
            this.AddData(IntrinioEconomicData, "$DCOILWTICO", Resolution.Daily);
            this.AddData(IntrinioEconomicData, "$DCOILBRENTEU", Resolution.Daily);
            this.emaWti = this.EMA("$DCOILWTICO", 10);
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object slice) {
            if (slice.ContainsKey("$DCOILBRENTEU") || slice.ContainsKey("$DCOILWTICO")) {
                var spread = slice["$DCOILBRENTEU"].Value - slice["$DCOILWTICO"].Value;
            } else {
                return;
            }
            if (spread > 0 && !this.Portfolio[this.bno].IsLong || spread < 0 && !this.Portfolio[this.uso].IsShort) {
                this.SetHoldings(this.bno, 0.25 * sign(spread));
                this.SetHoldings(this.uso, -0.25 * sign(spread));
            }
        }
    }
}
