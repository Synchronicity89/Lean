using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataFrameEtc
{
    class DataFrameS89 : DataFrame
    {
        public DataFrameS89(IEnumerable<DataFrameColumn> columns) : base(columns)
        {
        }

        public DataFrameS89(params DataFrameColumn[] columns) : base(columns)
        {
        }

        /// <summary>
        /// An indexer based on <see cref="DataFrameColumn.Name"/>
        /// </summary>
        /// <param name="columnName">The name of a <see cref="DataFrameColumn"/></param>
        /// <returns>A <see cref="DataFrameColumn"/> if it exists.</returns>
        /// <exception cref="ArgumentException">Throws if <paramref name="columnName"/> is not present in this <see cref="DataFrame"/></exception>
        public DataFrameColumn this[string columnName]
        {
            get => Columns[columnName];
            set => Columns[columnName] = value;
        }
    }
}
