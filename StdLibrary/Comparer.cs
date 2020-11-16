using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataFrameEtc
{
    public class Comparer
    {
        public List<Result> Results(FileInfo[] csvs)
        {
            DataFrame ordersdf = DataFrame.LoadCsv(csvs.First(csv => csv.Name.EndsWith("orders.csv")).FullName);
            DataFrame positionsdf = DataFrame.LoadCsv(csvs.First(csv => csv.Name.EndsWith("positions.csv")).FullName);
            //#error version
            PrimitiveDataFrameColumn<bool> orderAcct = ordersdf["Account"].ElementwiseEquals("U2831652");
            PrimitiveDataFrameColumn<bool> posAcct = positionsdf["Account"].ElementwiseEquals("U2831652");
            DataFrame filteredOrders = ordersdf.Filter(orderAcct);
            DataFrame filteredPositions = positionsdf.Filter(posAcct);

            Result result = new Result();
            return new List<Result>(new[] { result });
        }
    }

    public class Result
    {

    }
}
