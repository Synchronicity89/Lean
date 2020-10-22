from datetime import timedelta, datetime

class SMAPairsTrading(QCAlgorithm):

    def Initialize(self):
        # self.SetStartDate(2013, 3, 30)   
        # self.SetEndDate(2018, 3, 31)
        self.SetStartDate(2018, 9, 1)   
        self.SetEndDate(2019, 3, 31)
        self.SetCash(100000)
        symbols = []
        _symbols = ["MNST",     "SAM",     "PEP",     "BF.A",     "BF.B",     "KO",     "COKE",     "WVVI",     "FIZZ",     "AKO.A"]
        symbols = [Symbol.Create(s, SecurityType.Equity, Market.USA) for s in _symbols]
        self.AddUniverseSelection(ManualUniverseSelectionModel(symbols))
        self.UniverseSettings.Resolution = Resolution.Hour
        self.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw
        self.AddAlpha(PairsTradingAlphaModel())
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        
    def OnEndOfDay(self, symbol):
        self.Log("Taking a position of " + str(self.Portfolio[symbol].Quantity) + " units of symbol " + str(symbol))

class PairsTradingAlphaModel(AlphaModel):

    def __init__(self):
        self.pair = [ ]
        self.spreadMean = SimpleMovingAverage(500)
        self.spreadStd = StandardDeviation(500)
        self.period = timedelta(hours=2)
        
    def Update(self, algorithm, data):
        # spread = self.pair[1].Price - self.pair[0].Price
        ps = len([p for p in self.pair])
        spread = sum([x.Price for x in self.pair])/ps
        spreads = [x.Price/ps - spread for x in self.pair]
        self.spreadMean.Update(algorithm.Time, spread)
        self.spreadStd.Update(algorithm.Time, spread) 
        
        upperthreshold = self.spreadMean.Current.Value + self.spreadStd.Current.Value
        lowerthreshold = self.spreadMean.Current.Value - self.spreadStd.Current.Value
        insights = []
        for i in range(len(spreads)):
            insight = Insight.Price(self.pair[i].Symbol, self.period, InsightDirection.Up if spreads[i] > upperthreshold else InsightDirection.Down)
            insights.append(insight)
        return insights
    #   if spread > upperthreshold:
    #       return Insight.Group(
    #           [
    #               Insight.Price(self.pair[0].Symbol, self.period, InsightDirection.Up),
    #               Insight.Price(self.pair[1].Symbol, self.period, InsightDirection.Down)
    #           ])            
    
    #   if spread < lowerthreshold:
    #       return Insight.Group(
    #           [
    #               Insight.Price(self.pair[0].Symbol, self.period, InsightDirection.Down),
    #               Insight.Price(self.pair[1].Symbol, self.period, InsightDirection.Up)
    #           ])

        return []
    
    def OnSecuritiesChanged(self, algorithm, changes):
        self.pair = [x for x in changes.AddedSecurities]
        
        #1. Call for 500 bars of history data for each symbol in the pair and save to the variable history
        history = algorithm.History([x.Symbol for x in self.pair], 500)
        #2. Unstack the Pandas data frame to reduce it to the history close price
        history = history.close.unstack(0)
        #3. Iterate through the history tuple and update the mean and standard deviation with historical data
        for tupl in history.itertuples():
            self.spreadMean.Update(tupl[0], tupl[2]-tupl[1])
            self.spreadStd.Update(tupl[0], tupl[2]-tupl[1])