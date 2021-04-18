using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataFrameEtc
{
    public class Comparer
    {
        public List<string> Results(FileInfo[] csvs, int accountHint, string direction, int accountHint2)
        {
            DataFrame ordersdf = DataFrame.LoadCsv(csvs.First(csv => csv.Name.EndsWith("orders.csv")).FullName);
            DataFrame positionsdf = DataFrame.LoadCsv(csvs.First(csv => csv.Name.EndsWith("positions.csv")).FullName);

            var listOrderAccounts = new List<Int32>();
            foreach (var val in ordersdf["Account"])
            {
                listOrderAccounts.Add(int.Parse(val.ToString().Substring(1)));
            }
            var listPosAccounts = new List<Int32>();
            foreach (var val in positionsdf["Account"])
            {
                listPosAccounts.Add(int.Parse(val.ToString().Substring(1)));
            }
            var orderAccountNo = new Int32DataFrameColumn("AccountNo", listOrderAccounts);
            var posAccountNo = new Int32DataFrameColumn("AccountNo", listPosAccounts);
            ordersdf.Columns.Add(orderAccountNo);
            positionsdf.Columns.Add(posAccountNo);
            PrimitiveDataFrameColumn<bool> orderAcct = null;
            PrimitiveDataFrameColumn<bool> posAcct = null;
            PrimitiveDataFrameColumn<bool> orderAcct2 = null;
            PrimitiveDataFrameColumn<bool> posAcct2 = null;
            if (direction != "between")
            { 
                orderAcct = direction == "Above" ? ordersdf["AccountNo"].ElementwiseGreaterThan<Int32>(accountHint) :
                    ordersdf["AccountNo"].ElementwiseLessThan<Int32>(accountHint);
                posAcct = direction == "Above" ? positionsdf["AccountNo"].ElementwiseGreaterThan<Int32>(accountHint) :
                    positionsdf["AccountNo"].ElementwiseLessThan<Int32>(accountHint);
            }
            else
            {
                orderAcct = ordersdf["AccountNo"].ElementwiseGreaterThan<Int32>(accountHint);
                orderAcct2 = ordersdf["AccountNo"].ElementwiseLessThan<Int32>(accountHint2);
                posAcct = positionsdf["AccountNo"].ElementwiseGreaterThan<Int32>(accountHint);
                posAcct2 = positionsdf["AccountNo"].ElementwiseLessThan<Int32>(accountHint2);
            }
            //PrimitiveDataFrameColumn<bool> orderQuan = ordersdf["Quantity"].ElementwiseNotEquals(0.0f);
            //PrimitiveDataFrameColumn<bool> posQuan = positionsdf["Quantity"].ElementwiseNotEquals(0.0f);
            DataFrame filteredOrders = ordersdf.Filter(orderAcct);
            DataFrame filteredPositions = positionsdf.Filter(posAcct);
            if(direction == "between")
            {
                //TODO: this won't quite work yet, so debug later
                filteredOrders = ordersdf.Filter(orderAcct2);
                filteredPositions = positionsdf.Filter(posAcct2);
            }
            //filteredOrders = filteredOrders.Filter(orderQuan);
            //filteredPositions = filteredPositions.Filter(posQuan);
            PrimitiveDataFrameColumn<bool> ordersAction = filteredOrders["action"].ElementwiseEquals("SELL");
            filteredOrders = filteredOrders.Filter(ordersAction);
            filteredOrders = filteredOrders.OrderBy("Symbol");
            filteredPositions = filteredPositions.OrderBy("Symbol");
            posAcct = filteredPositions["Sec Type"].ElementwiseEquals("OPT");
            filteredPositions = filteredPositions.Filter(posAcct);
            //custom optional param only works with objects
            var newOrdersCol = filteredOrders["Symbol"].Add(filteredOrders["Quantity"], false, (object s) => (object)((string)s).Replace("-", ""));
            newOrdersCol.SetName("SymbolQ");
            filteredOrders.Columns.Add(newOrdersCol);
            var newPositionsCol = filteredPositions["Symbol"] + filteredPositions["Quantity"] ;
            newPositionsCol.SetName("SymbolQ");
            filteredPositions.Columns.Add(newPositionsCol);

            List<string> missing = new List<string>();

            foreach(var row in filteredPositions.Rows)
            {
                PrimitiveDataFrameColumn<bool> symbMatch = filteredOrders["SymbolQ"].ElementwiseEquals(row[filteredPositions.Columns.IndexOf("SymbolQ")].ToString());
                var symb = filteredOrders.Filter(symbMatch);
                if (symb.Rows.Count == 0) missing.Add(row[filteredPositions.Columns.IndexOf("SymbolQ")].ToString());
            }

            var merged = filteredPositions.Merge<string>(filteredOrders, "Symbol", "Symbol");
            missing.Clear();

            missing.Add("");
            missing.Add("Positions");
            missing.AddRange(filteredPositions.Rows.ToArray().Select(r => r.ToString()));
            missing.Add("");
            missing.Add("Orders");
            missing.AddRange(filteredOrders.Rows.ToArray().Select(r => r.ToString()));
            missing.Add("");
            missing.Add("Merged");
            missing.AddRange(merged.Rows.ToArray().Select(r => r.ToString()));
            return missing;
        }
    }

}
