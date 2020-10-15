
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using np = numpy;

using System.Collections.Generic;

public static class BasicTemplateFrameworkAlgorithm {
    
    static BasicTemplateFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Basic template framework algorithm uses framework components to define the algorithm.
    public class BasicTemplateFrameworkAlgorithm
        : QCAlgorithm {
        
        //  Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        public virtual object Initialize() {
            // Set requested data resolution
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            var symbols = new List<object> {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            };
            // set algorithm framework models
            this.SetUniverseSelection(ManualUniverseSelectionModel(symbols));
            this.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(minutes: 20), 0.025, null));
            // We can define who often the EWPCM will rebalance if no new insight is submitted using:
            // Resolution Enum:
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(Resolution.Daily));
            // timedelta
            // self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(timedelta(2)))
            // A lamdda datetime -> datetime. In this case, we can use the pre-defined func at Expiry helper class
            // self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(Expiry.EndOfWeek))
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01));
            this.Debug("numpy test >>> print numpy.pi: " + np.pi.ToString());
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            var deleteQueue = new List<Symbol>();

            foreach (var symbol in _currentSecurity.Keys)
            {
                var ticker = symbol.Value;
                try
                {
                    if (!data.ContainsKey(symbol))
                    {
                        continue;
                    }

                    var price = data[symbol].Close;

                    if (_currentSecurity[symbol].EntryPrice < price - price * 0.01m &&
                        _currentSecurity[symbol].BarsSinceEntry == 1)
                    {
                        SetHoldings(symbol, 0.1);
                    }
                    else if (price > _currentSecurity[symbol].ProfitPrice ||
                        price < _currentSecurity[symbol].StopPrice ||
                        _currentSecurity[symbol].BarsSinceEntry > 4)
                    {
                        Liquidate(symbol);
                        deleteQueue.Add(symbol);
                        _currentSecurity[symbol].BarsSinceEntry = 0;
                    }
                    else
                    {
                        if (_currentSecurity[symbol].BarsSinceEntry > 2)
                        {
                            UpdateADX(symbol);
                            _currentSecurity[symbol].StopPrice = price - _indicesADX[symbol].Atr.Current.Value / price * 2.5m;
                        }

                        _currentSecurity[symbol].BarsSinceEntry++;
                    }
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }
            }

            foreach (var symbol in deleteQueue)
            {
                _currentSecurity.Remove(symbol);

                _indices.Remove(symbol);
                _indicesADX.Remove(symbol);

                RemoveSecurity(symbol);
            }
        }

        public virtual object OnOrderEvent(object orderEvent) {
            if (orderEvent.Status == OrderStatus.Filled) {
                this.Debug("Purchased Stock: {0}".format(orderEvent.Symbol));
            }
        }
    }
}
