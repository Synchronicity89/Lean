import decimal as d

class CloneOfBasicTemplateAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2017,10,11)
        self.SetCash(5000)

        self.pair = self.AddForex("EURUSD").Symbol

    def OnData(self, data):
        
        if not self.Portfolio.Invested:
            price = data[self.pair].Close
            onePercent = d.Decimal(1.01)
            self.Buy(self.pair, 1000)
            self.LimitOrder(self.pair, -1000, price * onePercent)
            self.StopMarketOrder(self.pair, -1000, price / onePercent)

    def OnOrderEvent(self, orderEvent):
        order = self.Transactions.GetOrderById(orderEvent.OrderId)
        
        if order.Status == OrderStatus.Filled:
            if order.Type == OrderType.Limit or order.Type == OrderType.Limit:
                self.Transactions.CancelOpenOrders(order.Symbol)
                
        if order.Status == OrderStatus.Canceled:
            self.Log(str(orderEvent))