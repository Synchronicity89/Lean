class AlertBlackRabbit(QCAlgorithm):

    def Initialize(self):
        
        #Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Daily
        
        self.SetStartDate(2017, 4, 7)  # Set Start Date
        self.SetCash(100000)  # Set Strategy Cash

        # large cap 33%:
        largeCap = ["TTD", "ANET", "MDB", "TWLO", "FB", "ETSY", "VEEV", "TDOC", "TSLA", "DOCU", "HUBS", "DIS", "CRM", "ROKU", "PINS", "DDOG", "ATVI", "ISRG", "ALGN", "WDAY", "MNST", "CMG", "TWTR", "LYV", "LRCX", "AMZN", "NFLX", "MA", "NVDA", "CRWD", "ZS", "AAPL", "GOOG"]
        # international 29%
        international = ["SHOP", "MELI", "TEAM", "FVRR", "SE", "IQ", "LULU", "ASML", "BILI", "BIDU", "NTES", "TCEHY", "WIX", "JD", "ACN", "NVCR", "NXPI", "CYBR", "EDU", "BZUN", "FTCH", "CPNG", "DASTY", "LOGI", "NTDOY", "MOMO", "ALKS", "NICE", "NVO"]
        # Small Cap 19%
        smallCap=["PRLB", "BLUE", "CWH", "HA", "OII", "ANAB", "CLNE", "CWBHF", "DBI", "SAFT", "TNC"]
        # Mid Cap 13%
        midCap=["AYX", "AXON", "TTWO", "SFIX", "FSLY", "APPN", "ZEN", "UPWK", "UI", "NVTA", "ABMD", "AVAV", "AX", "PLNT", "TREX", "FRPT", "LMND", "FIVE", "GDRX", "CGNX", "BL", "MKL", "IRBT", "EDIT", "IPGP", "GH", "TRUP"]
        self.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin)
        securities = [Symbol.Create(x, SecurityType.Equity, Market.USA) for x in (smallCap + midCap)]

        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(days = 200), 0.025, 0.01))
        
        self.SetExecution(StandardDeviationExecutionModel(30, 0.15, Resolution.Daily))
        #self.SetExecution(ImmediateExecutionModel())

        #lambda time: None
        self.SetPortfolioConstruction(ConfidenceWeightedPortfolioConstructionModel(self.DateRules.MonthStart()));
        #self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());

        self.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.3))        
        self.SetUniverseSelection(ManualUniverseSelectionModel(securities))
        #taking first 5 for rapid debugging
        options = [self.AddOption(x, Resolution.Minute) for x in (largeCap + international)[:5]] 
        for option in options:
            days = numpy.random.ranint(100, 400)
            option.SetFilter(-3, 3, timedelta(days), timedelta(days + 40))

    def OnData(self, data):
        option_invested = [x.Key for x in self.Portfolio if x.Value.Invested and x.Value.Type==SecurityType.Option]
        
        if option_invested:
            if self.Time + timedelta(4) > option_invested[0].ID.Date:
                self.Liquidate(option_invested[0], "Too close to expiration")
            return
        
        if self.Securities[self.equity].Price >= self.high.Current.Value:
            for i in data.OptionChains:
                chains = i.Value
                self.BuyCall(chains)

    def BuyCall(self, chains):
        expiry = sorted(chains, key = lambda x: x.Expiry, reverse=True)[0].Expiry
        calls = [i for i in chains if i.Expiry == expiry and i.Right == OptionRight.Call]
        call_contracts = sorted(calls, key = lambda x: abs(x.Strike - x.UnderlyingLastPrice))
        if len(call_contracts) == 0:
            return
        self.call = call_contracts[0]
        
        quantity = self.Portfolio.TotalPortfolioValue / self.call.AskPrice
        quantity = int( 0.05 * quantity / 100)
        self.Buy(self.call.Symbol, quantity)
        
        
    def OnOrderEvent(self, orderEvent):
        order = self.Transactions.GetOrderById(orderEvent.OrderId)
        if order.Type == OrderType.OptionExercise:
            #liquadate anything with that symbol, needs to be tested/debugged
            for position in self.Portfolio:
                if str(position.Symbol).contains(order.Symbol.split()[0]):
                    position.Liquidate()
