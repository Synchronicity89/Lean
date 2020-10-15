
using AddReference = clr.AddReference;

using sleep = time.sleep;

public static class TrainingInitializeRegressionAlgorithm {
    
    static TrainingInitializeRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // Example algorithm showing how to use QCAlgorithm.Train method
    public class TrainingInitializeRegressionAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.AddEquity("SPY", Resolution.Daily);
            // this should cause the algorithm to fail
            // the regression test sets the time limit to 30 seconds and there's one extra
            // minute in the bucket, so a two minute sleep should result in RuntimeError
            this.Train(() => sleep(150));
            // DateRules.Tomorrow combined with TimeRules.Midnight enforces that this event schedule will
            // have exactly one time, which will fire between the first data point and the next day at
            // midnight. So after the first data point, it will run this event and sleep long enough to
            // exceed the static max algorithm time loop time and begin to consume from the leaky bucket
            // the regression test sets the "algorithm-manager-time-loop-maximum" value to 30 seconds
            this.Train(this.DateRules.Tomorrow, this.TimeRules.Midnight, () => sleep(60));
            // this will consume the single 'minute' available in the leaky bucket
        }
    }
}
