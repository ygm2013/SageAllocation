using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Sql;
using System.Data.SqlClient;

namespace sageAllocation
{
    public static class ClsRefData
    {
        private static string _year;
        private static string _month;
        private static string _batch;

        public static string alYear {
            get {
                return "20" + ClsRefData._year;
            }
        }
        public static string alPeriod
        {
            get
            {
                return ClsRefData._month;
            }
        }
        public static string alBatch
        {
            get
            {
                return "SJA"+ClsRefData._batch;
            }
        }
        public static void yrMon() {
              SqlConnection sqlCn = new SqlConnection("Server=sageserv;Database=demo;User Id=sa;Password=!firstel1;MultipleActiveResultSets=True");
            //SqlConnection sqlCn = new SqlConnection("Server=sage;Database=bluesat;User Id=sa;Password=admin@123;MultipleActiveResultSets=True");
            SqlCommand yrCmd = new SqlCommand("select max(key_value)  from scheme.sysdirm where system_key = 'SLYEAR'",sqlCn);
            SqlCommand monCmd = new SqlCommand("select max(key_value)  from scheme.sysdirm where system_key = 'SLPERIOD'", sqlCn);
            SqlCommand batchNumCmd = new SqlCommand("select max(next_batch)  from dbo.allocSysSet", sqlCn);

            sqlCn.Open();
            int _bch = (int)batchNumCmd.ExecuteScalar();
            ClsRefData._batch = _bch.ToString().PadLeft(3,'0');

            string _yr = (string)yrCmd.ExecuteScalar();
            ClsRefData._year = _yr;
            string _mon= (string)monCmd.ExecuteScalar();
            ClsRefData._month = _mon;

            SqlCommand nextBatchNumCmd = new SqlCommand("update dbo.allocSysSet set next_batch = next_batch + 1", sqlCn);
            nextBatchNumCmd.ExecuteNonQuery(); 

            sqlCn.Close();

            

        }
    }
    

}
