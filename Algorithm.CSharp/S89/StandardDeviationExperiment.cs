
namespace QuantConnect.Algorithm.CSharp
{
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    public class ModulatedOptimizedCircuit : QCAlgorithm
    {

        public override void Initialize()
        {
            SetStartDate(2019, 4, 21);  //Set Start Date
            SetCash(100000);             //Set Strategy Cash

            // AddEquity("SPY", Resolution.Minute);

            AddAlpha(new MacdAlphaModel());

            SetExecution(new StandardDeviationExecutionModel(10, 0.5m, Resolution.Hour));

            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            SetRiskManagement(new TrailingStopRiskManagementModel(0.03m));

            SetUniverseSelection(new QC500UniverseSelectionModel());

        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            // if (!Portfolio.Invested)
            // {
            //    SetHoldings("SPY", 1);
            //    Debug("Purchased Stock");
            //}
        }

    }
}
