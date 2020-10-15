
using AddReference = clr.AddReference;

using sleep = time.sleep;

using System.Collections.Generic;

public static class TrainingExampleAlgorithm {
    
    static TrainingExampleAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Example algorithm showing how to use QCAlgorithm.Train method
    public class TrainingExampleAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 14);
            this.AddEquity("SPY", Resolution.Daily);
            // Set TrainingMethod to be executed immediately
            this.Train(this.TrainingMethod);
            // Set TrainingMethod to be executed at 8:00 am every Sunday
            this.Train(this.DateRules.Every(DayOfWeek.Sunday), this.TimeRules.At(8, 0), this.TrainingMethod);
        }
        
        public virtual object TrainingMethod() {
            this.Log("Start training at {self.Time}");
            // Use the historical data to train the machine learning model
            var history = this.History(new List<string> {
                "SPY"
            }, 200, Resolution.Daily);
            // ML code:
        }
    }
}
