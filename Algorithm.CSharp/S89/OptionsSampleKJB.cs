using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{

    //from clr import AddReference
    //AddReference("System")
    //AddReference("QuantConnect.Algorithm")
    //AddReference("QuantConnect.Algorithm.Framework")
    //AddReference("QuantConnect.Common")

    //from System import*
    //from QuantConnect import *
    //from QuantConnect.Orders import*
    //from QuantConnect.Algorithm import *
    //from QuantConnect.Algorithm.Framework import *
    //from QuantConnect.Algorithm.Framework.Alphas import *
    //from QuantConnect.Algorithm.Framework.Portfolio import *
    //from QuantConnect.Algorithm.Framework.Selection import *
    //from Alphas.ConstantAlphaModel import ConstantAlphaModel
    //from Selection.OptionUniverseSelectionModel import OptionUniverseSelectionModel
    //from Execution.ImmediateExecutionModel import ImmediateExecutionModel
    //from Risk.NullRiskManagementModel import NullRiskManagementModel
    //from datetime import date, timedelta
    public static class C
    {
        public static double mult = 1;
        static  C()
        {
            if(Res == Resolution.Daily)
            {
                mult = 24 * 60;
            }
            else if(Res == Resolution.Hour)
            {
                mult = 60;
            }
        }
        public const Resolution Res = Resolution.Hour;
        public static Func<double, TimeSpan > Span = d => TimeSpan.FromMinutes(mult * d);

    }

    //class BasicTemplateOptionsFrameworkAlgorithm(QCAlgorithmFramework):
    class VirtualFluorescentPinkCaribou : QCAlgorithm
    {

        //    def Initialize(self):
        public override void Initialize()
        {
            //        self.UniverseSettings.Resolution = Resolution.Minute
            //UniverseSettings.Resolution = Resolution.Minute;
            UniverseSettings.Resolution = C.Res;

            //        self.SetStartDate(2020, 1, 1)
            SetStartDate(2020, 2, 18);
            //        self.SetEndDate(2020, 6, 1)
            SetEndDate(2020, 4, 14 );
            //        self.SetCash(100000)
            SetCash(100000);
            Func<string, Symbol> ToSymbol = x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA);
            var apeStock = new[] { "GME" }.Select(ToSymbol).ToArray();
            //        self.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelectionFunction))
            base.AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelectionFunction));
            //base.AddUniverseSelection(new ManualUniverseSelectionModel(apeStock));
            //        self.SetAlpha(ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(hours = 0.5)))
            //base.SetAlpha(new ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(30)));
            base.AddAlpha(new ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, C.Span(30)));
            //AddAlpha(new EmaCrossAlphaModel(50, 200, Resolution.Hour));

            //AddAlpha(new MacdAlphaModel(12, 26, 9, MovingAverageType.Simple, Resolution.Hour));

            //AddAlpha(new RsiAlphaModel(60, Resolution.Hour));
            //        self.SetPortfolioConstruction(SingleSharePortfolioConstructionModel())
            base.SetPortfolioConstruction(new SingleSharePortfolioConstructionModel());
            //        self.SetExecution(ImmediateExecutionModel())
            //base.SetExecution(new ImmediateExecutionModel());
            base.SetExecution(new StandardDeviationExecutionModel(60, 2.7m, C.Res));
            //        self.SetRiskManagement(NullRiskManagementModel())
            //base.SetRiskManagement(new NullRiskManagementModel());
            base.AddRiskManagement(new MaximumDrawdownPercentPerSecurity(Decimal.Parse(GetParameter("DrawdownPerSecurity"))));
            base.AddRiskManagement(new TrailingStopRiskManagementModel());
            base.SetWarmup(C.Span(60));
        }


        //    def CoarseSelectionFunction(self, coarse):
        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            //        sortedByDollarVolume = sorted(coarse, key= lambda x: x.DollarVolume, reverse= True)
            var sortedByDollarVolume = coarse.OrderBy(c => c.DollarVolume).Reverse();

            //        self.symbols = [x.Symbol for x in sortedByDollarVolume[:3] ]
            var symbols = sortedByDollarVolume.Take(3);

            //        for symbol in self.symbols:
            foreach (var symbol in symbols)
            {
                //            option = self.AddOption(symbol.Value)
                var option = AddOption(symbol.Symbol);
                //            option.SetFilter(-2, 2, timedelta(0), timedelta(182))
                option.SetFilter(-2, 2, TimeSpan.Zero, C.Span(182));
            }
            //        return self.symbols
            return symbols.Select(s => s.Symbol);
        }
    }

    //class ConstantOptionContractAlphaModel(ConstantAlphaModel):
    class ConstantOptionContractAlphaModel : ConstantAlphaModel
    {
        //    '''Implementation of a constant alpha model that only emits insights for option symbols'''
        //    def __init__(self, type, direction, period):
        //        super().__init__(type, direction, period)
        public ConstantOptionContractAlphaModel(InsightType type, InsightDirection direction, TimeSpan period) : base(type, direction, period)
        {
        }

        //    def ShouldEmitInsight(self, utcTime, symbol):
        protected override bool ShouldEmitInsight(DateTime utcTime, Symbol symbol)
        {
            //        # only emit alpha for option symbols and not underlying equity symbols
            //        if symbol.SecurityType != SecurityType.Option:
            if (symbol.SecurityType != SecurityType.Option)
            {
                //            return False
                return false;
            }
            //        return super().ShouldEmitInsight(utcTime, symbol)
            return base.ShouldEmitInsight(utcTime, symbol);
        }
    }


    //class SingleSharePortfolioConstructionModel(PortfolioConstructionModel):
    class SingleSharePortfolioConstructionModel : PortfolioConstructionModel
    {
        PortfolioBias _portfolioBias = PortfolioBias.LongShort;  //Change to long or short as desired

        //    '''Portoflio construction model that sets target quantities to 1 for up insights and -1 for down insights'''
        //    def CreateTargets(self, algorithm, insights):

        internal class TargetBlob
        {
            public decimal Count;
            public Insight Insight;
        }
        IEnumerable<PortfolioTarget> CreateTargets(QCAlgorithm algorithm, IEnumerable<Insight> insights)
        {
            var targetWeight = new Dictionary<string, TargetBlob>();
            //        targets = []
            List<PortfolioTarget> targets = new List<PortfolioTarget>();
            //        for insight in insights:
            foreach (var insight in insights)
            {
                string key = insight.Symbol.Value + insight.Direction.ToString();
                if(targetWeight.ContainsKey(key) == false)
                {
                    targetWeight.Add(key, new TargetBlob { Count = 1.0m, Insight = insight });
                }
                else
                {
                    targetWeight[key].Count++;
                }

            }
            //            targets.append(PortfolioTarget(insight.Symbol, insight.Direction))
            var max = targetWeight.OrderByDescending(kvp => kvp.Value).Take((int)(1.0 * targetWeight.Keys.Count));
            foreach (var kv in max)
            {
                targets.Add(new PortfolioTarget(kv.Value.Insight.Symbol, /* kv.Value.Count **/  (kv.Value.Insight.Direction == InsightDirection.Up ? .01m : -0.01m)));
            }
            //        return targets
            return targets;
            //    }
        }
        //The rest is borrowed from Equal
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            var result = new Dictionary<Insight, double>();

            // give equal weighting to each security
            var count = activeInsights.Count(x => x.Direction != InsightDirection.Flat && RespectPortfolioBias(x));
            var percent = count == 0 ? 0 : 1m / count;
            foreach (var insight in activeInsights)
            {
                result[insight] =
                    (double)((int)(RespectPortfolioBias(insight) ? insight.Direction : InsightDirection.Flat)
                             * percent);
            }
            return result;
        }

        /// <summary>
        /// Method that will determine if a given insight respects the portfolio bias
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>True if the insight respects the portfolio bias</returns>
        protected bool RespectPortfolioBias(Insight insight)
        {
            return _portfolioBias == PortfolioBias.LongShort || (int)insight.Direction == (int)_portfolioBias;
        }
    }
}