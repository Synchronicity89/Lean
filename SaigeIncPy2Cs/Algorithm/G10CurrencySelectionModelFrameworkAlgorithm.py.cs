
using AddReference = clr.AddReference;

using ManualUniverseSelectionModel = Selection.ManualUniverseSelectionModel.ManualUniverseSelectionModel;

using timedelta = datetime.timedelta;

using System.Linq;

using System.Collections.Generic;

public static class G10CurrencySelectionModelFrameworkAlgorithm {
    
    static G10CurrencySelectionModelFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Framework algorithm that uses the G10CurrencySelectionModel,
    //     a Universe Selection Model that inherits from ManualUniverseSelectionMode
    public class G10CurrencySelectionModelFrameworkAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // set algorithm framework models
            this.SetUniverseSelection(new G10CurrencySelectionModel());
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(minutes: 20), 0.025, null));
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01));
        }
        
        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Debug("Purchased Stock: {0}".format(orderEvent.Symbol));
            }
        }
        
        // Provides an implementation of IUniverseSelectionModel that simply subscribes to G10 currencies
        public class G10CurrencySelectionModel
            : ManualUniverseSelectionModel {
            
            public G10CurrencySelectionModel() {
            }
        }
    }
}
