namespace QuantConnect.Algorithm.CSharp
{
    using QuantConnect.Algorithm;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    public class EmaCrossAlphaModelX : EmaCrossAlphaModel
    {
        public EmaCrossAlphaModelX(int fastPeriod = 12, int slowPeriod = 26, Resolution resolution = Resolution.Daily) : base(fastPeriod, slowPeriod, resolution) { }
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var updates = base.Update(algorithm, data);
            //return updates;
            double? equal = 10.0 / (((SeparatedConcernEMAWeighted)algorithm).tickers.Count * 1.0);
            List<Insight> weighted = new List<Insight>();
            foreach (var update in updates)
            {
                Insight temp = null;
                if (update.Weight.HasValue == false || update.Weight.Value == 0.0)
                {
                    temp = new Insight(
                        update.Symbol,
                        update.Period,
                        update.Type,
                        update.Direction,
                        update.Magnitude,
                        update.Confidence,
                        update.SourceModel,
                        equal);
                }
                else
                {
                    temp = update;
                }
                weighted.Add(temp);
            }
            return weighted;
        }
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            base.OnSecuritiesChanged(algorithm, changes);
        }
    }

    public class StandardDeviationExecutionModelX : StandardDeviationExecutionModel
    {
        public StandardDeviationExecutionModelX(int period = 60, decimal deviations = 2, Resolution resolution = Resolution.Hour) : base(period, deviations, resolution) { }
        public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            base.Execute(algorithm, targets);
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            base.OnSecuritiesChanged(algorithm, changes);
        }
    }

    public class InsightWeightingPortfolioConstructionModelX : InsightWeightingPortfolioConstructionModel
    {
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            return base.CreateTargets(algorithm, insights);
        }
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            base.OnSecuritiesChanged(algorithm, changes);
        }
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            return base.DetermineTargetPercent(activeInsights);
        }
        protected override double GetValue(Insight insight)
        {
            return base.GetValue(insight);
            //return insight.Score.Direction;
        }
    }

    public class MaximumUnrealizedProfitPercentPerSecurityX : MaximumUnrealizedProfitPercentPerSecurity
    {
        public MaximumUnrealizedProfitPercentPerSecurityX(decimal maximumUnrealizedProfitPercent = 0.05M) : base(maximumUnrealizedProfitPercent) { }
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            return base.ManageRisk(algorithm, targets);
        }
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            base.OnSecuritiesChanged(algorithm, changes);
        }
    }

    public class SeparatedConcernEMAWeighted : QCAlgorithm
    {
        public Dictionary<int, Insight> insights = new Dictionary<int, Insight>();
        int i = 0;
        //random stock picks. replace these with your own picks
        public List<string> tickers = new List<string> { "GILD", "ABMD", "UNH", "ALXN", "BLK", "HFC", "KSU" };
        public override void Initialize()
        {
            SetStartDate(2015, 4, 5);  //Set Start Date
            SetCash(100000);             //Set Strategy Cash


            //         // AddEquity("SPY", Resolution.Hour);

            AddAlpha(new WeightedEmaCrossAlphaModel(50, 200, Resolution.Hour));

            SetExecution(new StandardDeviationExecutionModelX(60, 0.75m, Resolution.Hour));

            //SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            SetPortfolioConstruction(new InsightWeightingPortfolioConstructionModelX());

            //SetRiskManagement(new MaximumUnrealizedProfitPercentPerSecurityX(0.03m));
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.09m));


            UniverseSettings.Resolution = Resolution.Hour;

            var symbols = tickers.Select(t => QuantConnect.Symbol.Create(t, SecurityType.Equity, Market.USA)).ToArray();
            SetUniverseSelection(new LiquidETFUniverse());

            //SetUniverseSelection(new ManualUniverseSelectionModel(symbols));
            //InsightsGenerated += SeparatedConcernEMAWeighted_InsightsGenerated;

        }

        private void SeparatedConcernEMAWeighted_InsightsGenerated(Interfaces.IAlgorithm algorithm, GeneratedInsightsCollection eventData)
        {
            //Insight.CloseTimeUtc
            //Insight.Confidence
            //Insight.Direction
            //Insight.EstimatedValue
            //Insight.GeneratedTimeUtc
            //Insight.GroupId
            //Insight.Id
            //Insight.Magnitude
            //Insight.Period
            //Insight.ReferenceValue
            //Insight.ReferenceValueFinal
            //Insight.Score
            //Insight.SourceModel
            //Insight.Symbol
            //Insight.Type
            //Insight.Weight
            //this.EmitInsights

            foreach (var insight in eventData.Insights)
            {
                insights.Add(i++, insight);
            }
            //delete old insights
            var keys = new List<int>();
            foreach (var kvp in insights)
            {
                if (kvp.Value.IsActive(Time) == false)
                {
                    keys.Add(kvp.Key);
                }
            }
            keys.ForEach(k => insights.Remove(k));
        }

        public static OrderDirection ItoODir(InsightDirection direction)
        {
            switch (direction)
            {
                case InsightDirection.Down:
                    return OrderDirection.Sell;
                case InsightDirection.Up:
                    return OrderDirection.Buy;
                default:
                    return OrderDirection.Hold;
            }
        }

        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            // //foreach (var target in PortfolioConstruction.CreateTargets(this, insights.Values.ToArray()))
            // foreach(var insight in insights)
            // { 
            // 	if(data.ContainsKey(insight.Value.Symbol) == false) continue;
            //     var cash = Portfolio.Cash;
            //     var holding = Portfolio[insight.Value.Symbol];
            //     //holding.SetHoldings(holding.Price, Portfolio.GetMarginRemaining(insight.Value.Symbol, ItoODir(insight.Value.Direction)));
            //     if(insight.Value.Direction == InsightDirection.Up)
            //     {
            //     	SetHoldings(insight.Value.Symbol, 1.0m/tickers.Count);
            //     	//Debug(insight.Value.Symbol.Value + ": before; " + cash + ", after; " +  Portfolio.Cash);
            //     }
            //     if(insight.Value.Direction == InsightDirection.Down)
            //     {
            //     	SetHoldings(insight.Value.Symbol, -1.0m/tickers.Count);
            //     	//Debug(insight.Value.Symbol.Value + ": before; " + cash + ", after; " +  Portfolio.Cash);
            //     }
            // }
        }

        public override void OnWarmupFinished()
        {
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
        }
    }
}