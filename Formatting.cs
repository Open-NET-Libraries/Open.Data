using System;
using System.Data;

namespace Open.Data
{
    


    namespace Open.Data
    {
        public static class Formatting
        {

            /// <summary>
            /// Shortcut for validating if a DataTable contains any rows.
            /// </summary>
            public static bool Any(this DataTable table)
            {
                if (table == null) throw new NullReferenceException();
                return table.Rows.Count != 0;
            }


            public static double GetDouble(this DataRow target, string columnName)
            {
                if (target == null) throw new NullReferenceException();
                if (columnName == null) throw new ArgumentNullException("columnName");

                // Attempting to assure precision.
                if (target.IsNull(columnName))
                    return double.NaN;

                object value = target[columnName];
                return double.Parse(value.ToString());
            }

            public static float GetFloat(this DataRow target, string columnName)
            {
                if (target == null) throw new NullReferenceException();
                if (columnName == null) throw new ArgumentNullException("columnName");

                // Attempting to assure precision.
                if (target.IsNull(columnName))
                    return float.NaN;

                object value = target[columnName];
                return float.Parse(value.ToString());
            }
        }
    }

}
