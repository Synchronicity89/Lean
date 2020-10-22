//using QuantConnect;
//using QuantConnect.Algorithm;
//using QuantConnect.Algorithm.Framework.Alphas;
//using QuantConnect.Algorithm.Framework.Execution;
//using QuantConnect.Algorithm.Framework.Portfolio;
//using QuantConnect.Algorithm.Framework.Risk;
//using QuantConnect.Algorithm.Framework.Selection;
//using QuantConnect.Data;
//using QuantConnect.Data.UniverseSelection;
//using QuantConnect.Indicators;
//using QC = QuantConnect;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace SaigeIncCmdLine
//{
//    public partial class BootCampTask2 : QCAlgorithm
//    {
//        public override void Initialize()
//        {
//            SetStartDate(2013, 10, 1);
//            SetEndDate(2013, 12, 1);
//            SetCash(100000);

//            var symbols = new Symbol[] { QC.Symbol.Create("SPY", SecurityType.Equity, Market.USA), QC.Symbol.Create("BND", SecurityType.Equity, Market.USA) };

//            UniverseSettings.Resolution = Resolution.Daily;
//            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));
//            SetAlpha(new MOMAlphaModel());
//            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
//            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.15m));

//            //1. Set the Execution handler to a new instance of ImmediateExecutionModel()
//            SetExecution(new ImmediateExecutionModel());
//        }
//    }

//    public class MOMAlphaModel : AlphaModel
//    {
//        Dictionary<Symbol, Momentum> mom = new Dictionary<Symbol, Momentum>();

//        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
//        {
//            foreach (var security in changes.AddedSecurities)
//            {
//                var symbol = security.Symbol;
//                mom.Add(symbol, algorithm.MOM(symbol, 14, Resolution.Daily));
//            }
//        }

//        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
//        {
//            var ordered = mom.OrderByDescending(kvp => kvp.Value);

//            return Insight.Group(
//                Insight.Price(ordered.First().Key, TimeSpan.FromDays(1), InsightDirection.Up),
//                Insight.Price(ordered.Last().Key, TimeSpan.FromDays(1), InsightDirection.Flat)
//            );
//        }
//    }
//}
