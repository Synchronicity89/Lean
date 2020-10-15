
using AddReference = clr.AddReference;

public static class BasicSetAccountCurrencyAlgorithm {
    
    static BasicSetAccountCurrencyAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicSetAccountCurrencyAlgorithm
        : QCAlgorithm {
        
        public object _btcEur;
        
        // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            this.SetStartDate(2018, 4, 4);
            this.SetEndDate(2018, 4, 4);
            // Before setting any cash or adding a Security call SetAccountCurrency
            this.SetAccountCurrency("EUR");
            this.SetCash(100000);
            this._btcEur = this.AddCrypto("BTCEUR").Symbol;
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings(this._btcEur, 1);
            }
        }
    }
}
