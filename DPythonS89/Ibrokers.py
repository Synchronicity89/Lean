# Interactive Brokers functions to import data

def tws_time(): # request time to "wake-up" IB's API

    from datetime import datetime
    from threading import Thread
    import time

    from ibapi.client import EClient
    from ibapi.wrapper import EWrapper
    from ibapi.common import TickerId


    class ib_class(EWrapper, EClient):

        def __init__(self, addr, port, client_id):
            EClient. __init__(self, self)

            self.connect(addr, port, client_id) # Connect to TWS
            thread = Thread(target=self.run)  # Launch the client thread
            thread.start()

        def currentTime(self, cur_time):
            t = datetime.fromtimestamp(cur_time)
            print('Current TWS date/time: {}\n'.format(t))

        def error(self, reqId:TickerId, errorCode:int, errorString:str):
            if reqId > -1:
                print("Error. Id: " , reqId, " Code: " , errorCode , " Msg: " , errorString)

    ib_api = ib_class('127.0.0.1', 7497, 0)
    ib_api.reqCurrentTime() # associated callback: currentTime
    time.sleep(0.5)
    ib_api.disconnect()


def read_positions(): #read all accounts positions and return DataFrame with information

    from ibapi.client import EClient
    from ibapi.wrapper import EWrapper
    from ibapi.common import TickerId
    from threading import Thread

    import pandas as pd
    import time

    class ib_class(EWrapper, EClient):

        def __init__(self, addr, port, client_id):
            EClient.__init__(self, self)

            self.connect(addr, port, client_id) # Connect to TWS
            thread = Thread(target=self.run)  # Launch the client thread
            thread.start()

            self.all_positions = pd.DataFrame([], columns = ['Account','Symbol', 'Quantity', 'Average Cost', 'Sec Type'])

        def error(self, reqId:TickerId, errorCode:int, errorString:str):
            if reqId > -1:
                print("Error. Id: " , reqId, " Code: " , errorCode , " Msg: " , errorString)

        def position(self, account, contract, pos, avgCost):
            index = str(account)+str(contract.symbol) + contract.right + contract.lastTradeDateOrContractMonth if contract.lastTradeDateOrContractMonth != "" else str(contract)
            self.all_positions.loc[index]= account, contract.symbol, pos, avgCost, contract.secType

    ib_api = ib_class("127.0.0.1", 7497, 10)
    ib_api.reqPositions() # associated callback: position
    print("Waiting for IB's API response for accounts positions requests...\n")
    time.sleep(3.0)
    current_positions = ib_api.all_positions
    current_positions.set_index('Account',inplace=True,drop=True) #set all_positions DataFrame index to "Account"
    ib_api.disconnect()

    return(current_positions)
def read_orders(): #read all accounts orders and return DataFrame with information

    from ibapi.client import EClient
    from ibapi.wrapper import EWrapper
    from ibapi.common import TickerId
    from threading import Thread

    import pandas as pd
    import time

    class ib_class(EWrapper, EClient):
        printed = False
        def __init__(self, addr, port, client_id):
            EClient.__init__(self, self)

            self.connect(addr, port, client_id) # Connect to TWS
            thread = Thread(target=self.run)  # Launch the client thread
            thread.start()
#order.account, orderId, contract.symbol, order.totalQuantity, contract.secType, order.action, order.orderType, order.lmtPrice, order.tif
            self.all_orders = pd.DataFrame([], columns = ['Account','OrderId', 'Symbol', 'Quantity', 'Sec Type', 'action', 'orderType', 'lmtPrice', 'tif'])#])#

        def error(self, reqId:TickerId, errorCode:int, errorString:str):
            if reqId > -1:
                print("Error. Id: " , reqId, " Code: " , errorCode , " Msg: " , errorString)

#self, orderId: OrderId, contract: Contract, order: Order, orderState: OrderState
        def openOrder(self, orderId, contract, order, orderState):
            index = str(order.permId)+str(contract.symbol)+str(order.totalQuantity)+order.tif+str(order.lmtPrice)
            self.all_orders.loc[index]= order.account, orderId, contract.symbol, order.totalQuantity, contract.secType, order.action, order.orderType, order.lmtPrice, order.tif#
            if self.printed == False:
                #print(dir(order))
                print("orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice")
                self.printed = True

        def orderStatus(self, orderId: int, status: str, filled: float,
                    remaining: float, avgFillPrice: float, permId: int,
                    parentId: int, lastFillPrice: float, clientId: int,
                    whyHeld: str, mktCapPrice: float):
            #index = str(orderId)+str(contract.symbol)
            #self.all_orders.loc[index]= account, contract.symbol, ord, contract.secType
            print(str(orderId) + ", " + status + ", " + str(filled) + ", " + str(remaining) + ", " + str(avgFillPrice) + ", " + str(permId) + 
                ", " + str(parentId) + ", " + str(lastFillPrice) + ", " + str(clientId) + ", " + str(whyHeld) + ", " + str(mktCapPrice))

    ib_api = ib_class("127.0.0.1", 7497, 10)
    ib_api.reqAllOpenOrders() # associated callback: orders
    print("Waiting for IB's API response for accounts positions requests...\n")
    time.sleep(6.0)
    current_orders = ib_api.all_orders
    current_orders.set_index('Account',inplace=True,drop=True) #set all_positions DataFrame index to "Account"
    ib_api.disconnect()

    return(current_orders)


def read_navs(): #read all accounts NAVs

    from ibapi.client import EClient
    from ibapi.wrapper import EWrapper
    from ibapi.common import TickerId
    from threading import Thread

    import pandas as pd
    import time

    class ib_class(EWrapper, EClient):

        def __init__(self, addr, port, client_id):
            EClient.__init__(self, self)

            self.connect(addr, port, client_id) # Connect to TWS
            thread = Thread(target=self.run)  # Launch the client thread
            thread.start()

            self.all_accounts = pd.DataFrame([], columns = ['reqId','Account', 'Tag', 'Value' , 'Currency'])

        def error(self, reqId:TickerId, errorCode:int, errorString:str):
            if reqId > -1:
                print("Error. Id: " , reqId, " Code: " , errorCode , " Msg: " , errorString)

        def accountSummary(self, reqId, account, tag, value, currency):
            index = str(account)
            self.all_accounts.loc[index]=reqId, account, tag, value, currency

    ib_api = ib_class("127.0.0.1", 7497, 10)
    ib_api.reqAccountSummary(0,"All","NetLiquidation") # associated callback: accountSummary
    print("Waiting for IB's API response for NAVs requests...\n")
    time.sleep(3.0)
    current_nav = ib_api.all_accounts
    ib_api.disconnect()

    return(current_nav)

all_positions = read_positions()
all_navs = read_navs()
all_orders = read_orders()

import pandas

#df = pandas.DataFrame(app.data)
#df['DateTime'] = pandas.to_datetime(df['DateTime'],unit='s') 
all_positions.to_csv('..\\..\\all_positions.csv')  
all_navs.to_csv('..\\..\\all_navs.csv')  
all_orders.to_csv('..\\..\\all_orders.csv')  

print(all_positions)
print(all_navs)
