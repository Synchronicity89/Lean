using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaigeIncCmdLine
{
    public class LongShortEYAlphaModel : AlphaModel
    {
        private int _lastMonth = -1;

        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();

            //2. If statement to emit signals once a month
            if (_lastMonth == algorithm.Time.Month)
            {
                return insights;
            }
            _lastMonth = algorithm.Time.Month;

            //3. Use foreach to emit insights with insight directions 
            // based on whether earnings yield is greater or less than zero once a month
            foreach (var security in algorithm.ActiveSecurities.Values)
            {
                var _yield = security.Fundamentals.ValuationRatios.EarningYield;
                // Emit signals where the insight direction sign is based on the symbols' earnings yield 
                var direction = (InsightDirection)Math.Sign(_yield);
                insights.Add(Insight.Price(security.Symbol, TimeSpan.FromDays(28), direction));
            }
            return insights;
        }

        public partial class NewsSentimentAlphaModel : AlphaModel
        {
            private double _score;

            // Add word polarity scores and save to the variable wordsScores
            public Dictionary<string, double> wordScores = new Dictionary<string, double>()
        {
            {"attractive",0.5}, {"bad",-0.5}, {"beat",0.5}, {"beneficial",0.5},
            {"down",-0.5}, {"excellent",0.5}, {"fail",-0.5}, {"failed",-0.5}, {"good",0.5},
            {"great",0.5}, {"growth",0.5}, {"large",0.5}, {"lose",-0.5}, {"lucrative",0.5},
            {"mishandled",-0.5}, {"missed",-0.5}, {"missing",-0.5}, {"nailed",0.5},
            {"negative",-0.5}, {"poor",-0.5}, {"positive",0.5}, {"profitable",0.5},
            {"right",0.5}, {"solid",0.5}, {"sound",0.5}, {"success",0.5}, {"un_lucrative",-0.5},
            {"unproductive",-0.5}, {"up",0.5}, {"worthwhile",0.5}, {"wrong",-0.5}
        };

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                var insights = new List<Insight>();

                // 2. Access TiingoNews and save to the variable news
                var news = data.Get<TiingoNews>();

                foreach (var article in news.Values)
                {
                    // 3. Iterate through the article descriptions and save to the variable words
                    var words = article.Description.ToLower().Split(' ');

                    // 4. Assign a wordScore to the word if the word exists in wordScores and save to the variable _score 
                    _score = words
                        .Where(x => wordScores.ContainsKey(x))
                        .Sum(x => wordScores[x]);
                }

                return insights;
            }

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                foreach (var security in changes.AddedSecurities)
                {
                    // 1. When new assets are added to the universe
                    // request news data for the assets and save to variable newsAssets
                    var newsAsset = algorithm.AddData<TiingoNews>(security.Symbol);
                }
            }
        }
    }
    public class NewsData
    {
        public Symbol Symbol { get; }
        public RollingWindow<double> Window { get; }

        public NewsData(Symbol symbol)
        {
            Symbol = symbol;
            Window = new RollingWindow<double>(100);
        }
    }
    public partial class NewsSentimentAlphaModel : AlphaModel
    {
        private double _score;

        public Dictionary<Symbol, NewsData> _newsData = new Dictionary<Symbol, NewsData>();

        public Dictionary<string, double> wordScores = new Dictionary<string, double>()
        {
            {"attractive",0.5}, {"bad",-0.5}, {"beat",0.5}, {"beneficial",0.5},
            {"down",-0.5}, {"excellent",0.5}, {"fail",-0.5}, {"failed",-0.5}, {"good",0.5},
            {"great",0.5}, {"growth",0.5}, {"large",0.5}, {"lose",-0.5}, {"lucrative",0.5},
            {"mishandled",-0.5}, {"missed",-0.5}, {"missing",-0.5}, {"nailed",0.5},
            {"negative",-0.5}, {"poor",-0.5}, {"positive",0.5}, {"profitable",0.5},
            {"right",0.5}, {"solid",0.5}, {"sound",0.5}, {"success",0.5}, {"un_lucrative",-0.5},
            {"unproductive",-0.5}, {"up",0.5}, {"worthwhile",0.5}, {"wrong",-0.5}
        };

        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();
            var news = data.Get<TiingoNews>();

            foreach (var article in news.Values)
            {
                var words = article.Description.ToLower().Split(' ');
                _score = words
                    .Where(x => wordScores.ContainsKey(x))
                    .Sum(x => wordScores[x]);
            }
            return insights;
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var security in changes.AddedSecurities)
            {
                var symbol = security.Symbol;
                var newsAsset = algorithm.AddData<TiingoNews>(symbol);
                // 2. Create a new instance of the NewsData() and store in self.newsData[symbol]
                _newsData[symbol] = null;// new NewsData(newsAsset.Symbol);
            }

            foreach (var security in changes.RemovedSecurities)
            {
                // 3. Remove news data once assets are removed from our universe
                foreach (var ne in _newsData)
                {
                    if (ne.Key == security.Symbol)
                    {
                        _newsData.Remove(ne.Key);
                    }
                }
            }
        }

    }

    public partial class BullPutSpread : QCAlgorithm
    {
        private Symbol _optionSymbol;
        string iSymbol = "MSFT";
        DateTime iTime;

        public override void Initialize()
        {
            AddSecurity(SecurityType.Option, "MSFT", Resolution.Minute);
            SetStartDate(2015, 12, 24);
            SetEndDate(2016, 12, 23);
            SetCash(1000000);

            var option = AddOption("MSFT");
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(-2, +2, TimeSpan.Zero, TimeSpan.FromDays(180));

            // Adding this to reproduce GH issue #2314
            SetWarmup(TimeSpan.FromMinutes(1));

            // use the underlying equity as the benchmark
            base.SetBenchmark("MSFT");
            AddEquity(iSymbol, Resolution.Minute);
        }

        public void OnData(TradeBars data)
        {
            if (IsMarketOpen(iSymbol) == false)
            {
                return;
            }

            if (IsNewBar(TimeSpan.FromHours(1)) == false)
            {
                return;
            }

            var price = Securities[iSymbol].Price;

            // If options were exercised and we were assigned to buy shares, sell them immediately

            if (Portfolio[iSymbol].Invested)
            {
                MarketOrder(iSymbol, -10000);
            }

            if (Portfolio.Invested == false)
            {
                var contracts = OptionChainProvider.GetOptionContractList(iSymbol, Time);

                // Choose all contracts within a month and strike price $1 to $5 from current underlying price

                var atmPuts =
                    from c in contracts
                    where c.ID.OptionRight == OptionRight.Put
                    where price - c.ID.StrikePrice < 3 && price - c.ID.StrikePrice > 1
                    where (c.ID.Date - Time).TotalDays < 45 && (c.ID.Date - Time).TotalDays > 0
                    select c;

                // Choose all contracts within a month and strike price $1 to $5 from current underlying price

                var otmPuts =
                    from c in contracts
                    where c.ID.OptionRight == OptionRight.Put
                    where price - c.ID.StrikePrice < 7 && price - c.ID.StrikePrice > 5
                    where (c.ID.Date - Time).TotalDays < 45 && (c.ID.Date - Time).TotalDays > 0
                    select c;

                // Take ATM options with the MIN expiration date and MAX distance from underlying price

                var contractAtmPut = atmPuts
                    .OrderBy(o => o.ID.Date)
                    .ThenBy(o => price - o.ID.StrikePrice)
                    .FirstOrDefault();

                // Take OTM options with the MIN expiration date and MAX distance from underlying price

                var contractOtmPut = otmPuts
                    .OrderBy(o => o.ID.Date)
                    .ThenBy(o => price - o.ID.StrikePrice)
                    .FirstOrDefault();

                // If we found such options - open trade

                if (contractAtmPut != null &&
                    contractOtmPut != null)
                {
                    AddOptionContract(contractAtmPut, Resolution.Minute);
                    AddOptionContract(contractOtmPut, Resolution.Minute);
                    MarketOrder(contractAtmPut, -1);
                    MarketOrder(contractOtmPut, 1);
                }
            }
        }

        public bool IsNewBar(TimeSpan interval, int points = 1)
        {
            var date = Securities[_optionSymbol].LocalTime;

            if ((date - iTime).TotalSeconds > interval.TotalSeconds * points)
            {
                iTime = new DateTime(date.Ticks - date.Ticks % interval.Ticks, date.Kind);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            //Log(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "778"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-100%"},
            {"Drawdown", "6.900%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-6.860%"},
            {"Sharpe Ratio", "0"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$778.00"}
        };
    }

    public class OptionsUniverseSelectionModel : OptionUniverseSelectionModel
    {
        Func<DateTime, IEnumerable<Symbol>> selector = dt => new Symbol[] { };
        //private OptionsUniverseSelectionModel() {}
        public OptionsUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> select_option_chain_symbols) : base(TimeSpan.FromMinutes(1), select_option_chain_symbols)
        {
            
        }

        protected override OptionFilterUniverse Filter(OptionFilterUniverse filter)
        {
            // Define options filter -- strikes +/- 3 and expiry between 0 and 180 days away
            return (filter.Strikes(-20, +20)
                          .Expiration(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(30)));
        }
    }

}
