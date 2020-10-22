class LiquidUniverseSelection(QCAlgorithm):
    
    filteredByPrice = None
    tickets = {}
    
    def Initialize(self):
        self.SetStartDate(2018, 2, 1)  
        self.SetEndDate(2018, 8, 1) 
        # self.SetStartDate(2019, 1, 11)  
        # self.SetEndDate(2019, 7, 1) 
        self.SetCash(100000)  
        self.AddUniverse(self.CoarseSelectionFilter)
        self.UniverseSettings.Resolution = Resolution.Daily

        #1. Set the leverage to 2
        self.UniverseSettings.Leverage = 2.0
    
    def CoarseSelectionFilter(self, coarse):
        sortedByDollarVolume = sorted(coarse, key=lambda c: c.DollarVolume, reverse=True)
        self.filteredByPrice = [c.Symbol for c in sortedByDollarVolume if c.Price > 10]
        return self.filteredByPrice[:10] 

    def OnSecuritiesChanged(self, changes):
        self.changes = changes
        self.Log(f"OnSecuritiesChanged({self.Time}):: {changes}")
        
        for security in self.changes.RemovedSecurities:
            if security.Invested:
                #self.Liquidate(security.Symbol)
                q = security.Holdings.Quantity
                i = 1.01 if q > 0 else 0.99
                if not security.Symbol in self.tickets:
                    self.tickets[security.Symbol] = None
                if not self.tickets[security.Symbol] is None:
                    self.tickets[security.Symbol].Cancel("new Liquid")
                limitTicket = self.LimitOrder(security.Symbol, -1 * q, security.Price * i)
                self.tickets[security.Symbol] = limitTicket
                
        for security in self.changes.AddedSecurities:
            #2. Leave a cash buffer by setting the allocation to 0.18 instead of 0.2 
            # self.SetHoldings(security.Symbol, ...)
            self.SetHoldings(security.Symbol, 0.18)
            
    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            #self.Debug(f'Purchased Stock: {orderEvent.Symbol}')
            ticket = self.Transactions.GetOrderTicket(orderEvent.OrderId)
            self.tickets[ticket.Symbol] = None