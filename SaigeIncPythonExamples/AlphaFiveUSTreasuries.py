from QuantConnect.Data.Custom.TradingEconomics import *
#https://www.quantconnect.com/forum/discussion/6747/the-economy-stupid-using-trading-economics-in-your-algorithms/p1
class AlphaFiveUSTreasuries(QCAlgorithm):

    def Initialize(self):

        #1. Required: Five years of backtest history
        self.SetStartDate(2014, 1, 1)

        #2. Required: Alpha Streams Models:
        self.SetBrokerageModel(BrokerageName.AlphaStreams)
    
        #3. Required: Significant AUM Capacity
        self.SetCash(1000000)

        #4. Required: Benchmark to SPY
        self.SetBenchmark("SPY")
        
        #5. Use InsightWeightingPCM since we will compute the weights
        self.SetPortfolioConstruction(InsightWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())

        # Add TradingEconomicsCalendar for Energy Data
        us = TradingEconomics.Calendar.UnitedStates
        self.nat = self.AddData(TradingEconomicsCalendar, us.NaturalGasStocksChange).Symbol
        self.oli = self.AddData(TradingEconomicsCalendar, us.ApiCrudeOilStockChange).Symbol
        self.gas = self.AddData(TradingEconomicsCalendar, us.GasolineStocksChange).Symbol

        # Energy Basket 
        tickers = ["XLE", "IYE", "VDE", "USO", "XES", "XOP",
                   "UNG", "ICLN", "ERX", "ERY", "SCO", "UCO",
                   "AMJ", "BNO", "AMLP", "OIH", "DGAZ", "UGAZ", "TAN"]

        # Add Equity ---------------------------------------------- 
        self.symbols = [self.AddEquity(x).Symbol for x in tickers]

        self.factor = 0
        
        # Emit insights 10 minutes after market open to
        # try to ensure all price data is from the current day
        self.Schedule.On(self.DateRules.EveryDay("XLE"), 
                         self.TimeRules.AfterMarketOpen("XLE", 10),
                         self.EveryDayAfterMarketOpen)

    def EveryDayAfterMarketOpen(self):
        if self.factor == 0:
            return

        # The weight is factor normialized by the number of symbols
        weight = self.factor / len(self.symbols)
        self.factor = 0

        # Emit Up Price insight
        self.EmitInsights([
            Insight.Price(x, timedelta(15), InsightDirection.Up, None, None, None, weight)
                for x in self.symbols])


    def OnData(self, data):

        # Discard updates before 10 to avoid EveryDayAfterMarketOpen running with today's data
        if self.Time.hour < 10:
            return

        # Compute the factor based on the Actual vs Forecast values
        for kvp in data.Get(TradingEconomicsCalendar):
            calendar = kvp.Value

            actual = calendar.Actual
            
            # The reference will be the Forecast, but if not available, use the Previous
            reference = calendar.Forecast
            if reference is None or reference == 0:
                reference = calendar.Previous
            if reference is None or reference == 0:
                reference = actual

            # Actual was worse than the reference.
            # Bad. Reduce all positions to a minimum
            if actual < reference:
                self.factor = 0.1
                continue

            self.factor = max(0.1, min(1, 1 - actual / reference))
