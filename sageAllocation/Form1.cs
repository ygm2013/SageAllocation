using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data.Sql;


namespace sageAllocation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }



        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int i = 1;
            string prd = ClsRefData.alPeriod;
            string yr = ClsRefData.alYear;

            string control_acc = "01-1-66-001";
            //string batchNo = "SJA080";
            string[] itemNumber = new string[31];
            Double[] itemAmount = new Double[31];

            string[] itemNumberCr = new string[31];
            Double[] itemAmountCr = new Double[31];

            string[] itemNumberDr = new string[31];
            Double[] itemAmountDr = new Double[31];

            //string [] allCustomer;
            //create the connection to the database and open it
             SqlConnection sqlCon = new SqlConnection("Server=sageserv;Database=firstel;User Id=sa;Password=!firstel1;MultipleActiveResultSets=True");
            //SqlConnection sqlCon = new SqlConnection("Server=sage;Database=bluesat;User Id=sa;Password=admin@123;MultipleActiveResultSets=True");
            SqlCommand cntCustCmd = new SqlCommand("select count(distinct customer) cnt from scheme.slitemm where unall_amount < 0 and open_indicator = 'O' " +
                                                " and customer between @startAcc1 and @endAcc1", sqlCon);
            SqlCommand allCustCmd = new SqlCommand("  select customer,name,alpha "+
                                                   " from scheme.slcustm  "+
                                                   " where customer in (select customer "+
						                                                " from scheme.customer_list ) " +
                                                    " and customer between @startAcc and @endAcc "+
                                                    " and customer not in (select customer from dbo.exclAlloc)"+
                                                    " order by customer ", sqlCon);

            
            // create temporary table for all open items , drop the table if it exists
            SqlCommand dropCmd = new SqlCommand("drop table scheme.tmpslitem ", sqlCon);

            SqlCommand InsertCmd = new SqlCommand("select customer,item_no,unall_amount,dated,open_indicator,unall_amount cons_amt,open_indicator partial_ind ,'U' drCr_ind " +
                                                  " into scheme.tmpslitem " + 
                                                   " from scheme.slitemm " +
                                                  " where unall_amount != 0 and open_indicator = 'O' "+
                                                   " and customer between @startAcc1 and @endAcc1 " +
                                                   " and customer not in (select customer from dbo.exclAlloc)", sqlCon);
            sqlCon.Open();
            dropCmd.ExecuteNonQuery();
            InsertCmd.Parameters.AddWithValue("@startAcc1", txtStartAcc.Text);
            InsertCmd.Parameters.AddWithValue("@endAcc1", txtEndAcc.Text);
            InsertCmd.ExecuteNonQuery();// finshed creating list of custmers to be processed 
            sqlCon.Close();// finshed creating list of custmers to be processed 

            allCustCmd.Parameters.AddWithValue("@startAcc", txtStartAcc.Text);
            allCustCmd.Parameters.AddWithValue("@endAcc", txtEndAcc.Text);
            try
            {
                sqlCon.Open();
                cntCustCmd.Parameters.AddWithValue("@startAcc1", txtStartAcc.Text);
                cntCustCmd.Parameters.AddWithValue("@endAcc1", txtEndAcc.Text);
                int cnt = (int)cntCustCmd.ExecuteScalar();// all customers
               // MessageBox.Show(" " + cnt);

                SqlDataReader myreader = allCustCmd.ExecuteReader();// all customers
                // 
               
                while (myreader.Read()) // outer loops over list of customer with unallocated paymnets
                {
                   // int xDbt = 0;
                    //int xCrd = 0;
                    for (int jdx = 0; jdx < 31; jdx++)
                    {
                        itemAmount[jdx] = 0;
                        itemNumber[jdx] = "     ";
                    }
                    SqlCommand credCmd = new SqlCommand("select customer,item_no,unall_amount from scheme.tmpslitem  " +
                                                " where unall_amount < 0 and open_indicator = 'O' " +
                                                " and customer = @credAcc" , sqlCon);

                    SqlCommand credCntCmd = new SqlCommand("select  sum(unall_amount) amt from scheme.tmpslitem  " +
                                               " where unall_amount < 0 and open_indicator = 'O' " +
                                               " and customer = @credAcc1  "  , sqlCon);
                  // MessageBox.Show("Now processing  :  " + myreader["customer"].ToString());
                    credCntCmd.Parameters.AddWithValue("@credAcc1", myreader["customer"].ToString());
                    double cntCr = Math.Abs((double)credCntCmd.ExecuteScalar());// all credits for a customers
                    //MessageBox.Show(" Total Credits |  "+cntCr);

                    credCmd.Parameters.AddWithValue("@credAcc", myreader["customer"].ToString());
                    SqlDataReader credReader = credCmd.ExecuteReader(); // credits for a particular customer.
                    //MessageBox.Show("  "+i+ "  " + cnt);

                    i++; 
                    //progressBar1.Value = i / cnt * 100;
                    //progressBar1.PerformStep();
                    Double tmpCred = 0.0;
                    try
                    {
                        while (credReader.Read())
                        {
                            tmpCred = 0.0;
                                                    
                            SqlCommand debitCmd = new SqlCommand("select customer,item_no,unall_amount from scheme.tmpslitem  " +
                                                 " where unall_amount > 0 and open_indicator = 'O' " +
                                                 " and customer = @dbtAcc " +
                                                 " order by dated ", sqlCon);
                            debitCmd.Parameters.AddWithValue("@dbtAcc",credReader["customer"].ToString());
                            SqlDataReader debitReader = debitCmd.ExecuteReader(); // debits for the current customer.
                            tmpCred = tmpCred + Math.Abs((double)credReader["unall_amount"]);
                           // MessageBox.Show(" Credits  :  " + tmpCred);
                            //flag = 0; 
                                try
                                {
                                    //while (tmpCred > 0)  // here we keep track of one credit at time.
                                    //{
                                    //    if (flag == 1) { break; }
                                        //MessageBox.Show(" "+tmpCred);
                                        while (debitReader.Read())
                                        {
                                            if (tmpCred == 0) { break; }
                                        // index to position the items, we place
                                        //MessageBox.Show(" Inside debits loop | Credits : " + cntCr);
                                        if (((double)debitReader["unall_amount"] <= tmpCred))
                                        {
                                           // MessageBox.Show(" 1. Inside debits loop | Credits : <= " + debitReader["unall_amount"] + " Credits  :  " + tmpCred );
                                           // cntCr = cntCr - (double)debitReader["unall_amount"];
                                           tmpCred = tmpCred - (double)debitReader["unall_amount"];

                                            SqlCommand updDb = new SqlCommand("update scheme.tmpslitem  "+
                                                                            "set open_indicator = 'C',partial_ind = 'C', unall_amount= 0, drCr_ind = 'D' " +
                                                                            "where item_no = @itemNo " +
                                                                            " and customer = @cust ", sqlCon);
                                            updDb.Parameters.AddWithValue("@itemNo", debitReader["item_no"]);
                                            updDb.Parameters.AddWithValue("@cust", debitReader["customer"]);
                                            //MessageBox.Show("Updating   : " + debitReader["customer"].ToString());
                                            updDb.ExecuteNonQuery();

                                            SqlCommand updCr = new SqlCommand("update scheme.tmpslitem  " +
                                                                           "set partial_ind = 'P',unall_amount = @tmpCred , drCr_ind = 'C'" +
                                                                           "where item_no = @itemNo " +
                                                                           " and customer = @cust ", sqlCon);
                                            updCr.Parameters.AddWithValue("@tmpCred",tmpCred*-1);
                                            updCr.Parameters.AddWithValue("@itemNo", credReader["item_no"]);
                                            updCr.Parameters.AddWithValue("@cust", debitReader["customer"]);
                                            //MessageBox.Show("Updating   : " + debitReader["customer"].ToString());
                                            updCr.ExecuteNonQuery();
                                        }
                                        else if (((double)debitReader["unall_amount"] > tmpCred))
                                        {
                                            //MessageBox.Show(" 1. Inside debits loop | Credits : <= " + debitReader["unall_amount"] + " Credits  :  " + tmpCred );
                                            // cntCr = cntCr - (double)debitReader["unall_amount"];
                                            //tmpCred = 0; //tmpCred - (double)debitReader["unall_amount"];

                                            SqlCommand updDb = new SqlCommand("update scheme.tmpslitem  " +
                                                                            "set  partial_ind = 'P',unall_amount = @drAmt, drCr_ind= 'D' " +
                                                                            "where item_no = @itemNo " +
                                                                            " and customer = @cust ", sqlCon);
                                            updDb.Parameters.AddWithValue("@drAmt", (double)debitReader["unall_amount"] - tmpCred);
                                            updDb.Parameters.AddWithValue("@itemNo", debitReader["item_no"]);
                                            updDb.Parameters.AddWithValue("@cust", debitReader["customer"]);
                                            //MessageBox.Show("Updating   : " + debitReader["customer"].ToString());
                                            updDb.ExecuteNonQuery();

                                            tmpCred = 0;
                                            SqlCommand updCr = new SqlCommand("update scheme.tmpslitem  " +
                                                                           "set open_indicator = 'C',partial_ind = 'C',unall_amount = 0, drCr_ind = 'C' " +
                                                                           "where item_no = @itemNo " +
                                                                           " and customer = @cust ", sqlCon);
                                            //updCr.Parameters.AddWithValue("@tmpCred", tmpCred * -1);
                                            updCr.Parameters.AddWithValue("@itemNo", credReader["item_no"]);
                                            updCr.Parameters.AddWithValue("@cust", debitReader["customer"]);
                                            //MessageBox.Show("Updating   : " + debitReader["customer"].ToString());
                                            updCr.ExecuteNonQuery();
                                        }
                                        
                                        //xDbt++;
                                    } // exit this loop to get more credits 
                                }
                                //}
                                finally
                                {
                                    debitReader.Dispose();
                                    debitReader.Close();
                                }// exit 
                           
                           /* if (cntCr <= 0)
                            {
                                break;
                            }*/
                        }// credits for one customer , exit and fetch another customer when there are no more credits

                       
                    }
                    finally
                    {
                        //MessageBox.Show("Credit outer ");
                        credReader.Dispose();
                        credReader.Close();
                    }

                }

            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                sqlCon.Close();
            }

            // save allocation to sales journal ((sljrnm)
            SqlCommand selCmnd = new SqlCommand(" select customer, name,alpha from scheme.slcustm"+
                                                " where customer in (select distinct customer "+
					                            " from scheme.tmpslitem "+
					                            " where partial_ind in ('P','C')) ", sqlCon);
            sqlCon.Open();
            SqlDataReader procReader = selCmnd.ExecuteReader();
            int dbCnt = 0; //int //crCnt = 0;
            while (procReader.Read())
            {
                SqlCommand detCmnd = new SqlCommand(" select customer,item_no,cons_amt - unall_amount item_amt ,drCr_ind "+
                                                    " from scheme.tmpslitem "+
                                                    " where partial_ind in ('P','C') "+
                                                    " and customer = @custm" +
                                                    " order by drCr_ind", sqlCon);
                detCmnd.Parameters.AddWithValue("@custm", procReader["customer"]);
                SqlDataReader detReader = detCmnd.ExecuteReader();
                dbCnt = 0;  //crCnt = 0;
                while (detReader.Read())
                {
                   // if (detReader["drCr_ind"].ToString().Equals("D")) //InsertCmd debits
                   // {
                        itemAmount[dbCnt] = (double)detReader["item_amt"];
                        itemNumber[dbCnt++] = detReader["item_no"].ToString();
                   // }
                }
                // insert the journal record  
                DateTime now = DateTime.Now;
                DateTime firstOfNextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                DateTime lastOfThisMonth = firstOfNextMonth.AddDays(-1);

                // MessageBox.Show("Date " + now.ToString("d") + "Date " + now.ToString("T").Substring(0,8));

                SqlCommand insJrnCmd = new SqlCommand("insert into scheme.sljrnm " +
                                                        "(batch,account_no,item,page_no,dated,name,alpha,item_numbers02,item_numbers03,item_numbers04 " +
                                                        ",item_numbers05,item_numbers06,item_numbers07,item_numbers08,item_numbers09,item_numbers10 " +
                                                        ",item_numbers11,item_numbers12,item_numbers13,item_numbers14,item_numbers15,item_numbers16 " +
                                                        ",item_numbers17,item_numbers18,item_numbers19,item_numbers20,item_numbers21,item_numbers22 " +
                                                        ",item_numbers23,item_numbers24,item_numbers25,item_numbers26,item_numbers27,item_numbers28 " +
                                                        ",item_numbers29,item_numbers30,item_numbers31,item_amounts02,item_amounts03,item_amounts04 " +
                                                        ",item_amounts05,item_amounts06,item_amounts07,item_amounts08,item_amounts09,item_amounts10 " +
                                                        ",item_amounts11,item_amounts12,item_amounts13,item_amounts14,item_amounts15,item_amounts16 " +
                                                        ",item_amounts17,item_amounts18,item_amounts19,item_amounts20,item_amounts21,item_amounts22 " +
                                                        ",item_amounts23,item_amounts24,item_amounts25,item_amounts26,item_amounts27,item_amounts28 " +
                                                        ",item_amounts29,item_amounts30,item_amounts31,username,userdate,usertime,effective_date,period " +
                                                        ",slyear,control,posting_ind )  " +
                                                        "values " +
                                                        "( @batch,@account_no,@item,@page_no,@dated,@name,@alpha,@item_numbers02,@item_numbers03,@item_numbers04 " +
                                                        ",@item_numbers05,@item_numbers06,@item_numbers07,@item_numbers08,@item_numbers09,@item_numbers10 " +
                                                        ",@item_numbers11,@item_numbers12,@item_numbers13,@item_numbers14,@item_numbers15,@item_numbers16 " +
                                                        ",@item_numbers17,@item_numbers18,@item_numbers19,@item_numbers20,@item_numbers21,@item_numbers22 " +
                                                        ",@item_numbers23,@item_numbers24,@item_numbers25,@item_numbers26,@item_numbers27,@item_numbers28 " +
                                                        ",@item_numbers29,@item_numbers30,@item_numbers31,@item_amounts02,@item_amounts03,@item_amounts04 " +
                                                        ",@item_amounts05,@item_amounts06,@item_amounts07,@item_amounts08,@item_amounts09,@item_amounts10 " +
                                                        ",@item_amounts11,@item_amounts12,@item_amounts13,@item_amounts14,@item_amounts15,@item_amounts16 " +
                                                        ",@item_amounts17,@item_amounts18,@item_amounts19,@item_amounts20,@item_amounts21,@item_amounts22 " +
                                                        ",@item_amounts23,@item_amounts24,@item_amounts25,@item_amounts26,@item_amounts27,@item_amounts28 " +
                                                        ",@item_amounts29,@item_amounts30,@item_amounts31,@username,@userdate,@usertime,@effective_date,@period " +
                                                        ",@slyear,@control,@posting_ind ) ", sqlCon);
                /******      *****/


                insJrnCmd.Parameters.AddWithValue("@batch", ClsRefData.alBatch);
                insJrnCmd.Parameters.AddWithValue("@account_no", procReader["customer"]);
                insJrnCmd.Parameters.AddWithValue("@item", ClsRefData.alBatch);
                insJrnCmd.Parameters.AddWithValue("@page_no", "/001");
                insJrnCmd.Parameters.AddWithValue("@dated", now.ToString("d"));
                insJrnCmd.Parameters.AddWithValue("@name", procReader["name"]);
                insJrnCmd.Parameters.AddWithValue("@alpha", procReader["alpha"]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers02", itemNumber[0]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers03", itemNumber[1]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers04", itemNumber[2]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers05", itemNumber[3]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers06", itemNumber[4]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers07", itemNumber[5]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers08", itemNumber[6]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers09", itemNumber[7]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers10", itemNumber[8]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers11", itemNumber[9]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers12", itemNumber[10]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers13", itemNumber[11]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers14", itemNumber[12]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers15", itemNumber[13]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers16 ", itemNumber[14]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers17", itemNumber[15]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers18", itemNumber[16]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers19", itemNumber[17]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers20", itemNumber[18]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers21", itemNumber[19]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers22 ", itemNumber[20]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers23", itemNumber[21]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers24", itemNumber[22]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers25", itemNumber[23]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers26", itemNumber[24]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers27", itemNumber[25]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers28 ", itemNumber[26]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers29", itemNumber[27]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers30", itemNumber[28]);
                insJrnCmd.Parameters.AddWithValue("@item_numbers31", itemNumber[29]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts02", itemAmount[0]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts03", itemAmount[1]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts04 ", itemAmount[2]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts05", itemAmount[3]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts06", itemAmount[4]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts07", itemAmount[5]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts08", itemAmount[6]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts09", itemAmount[7]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts10 ", itemAmount[8]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts11", itemAmount[9]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts12", itemAmount[10]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts13", itemAmount[11]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts14", itemAmount[12]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts15", itemAmount[13]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts16 ", itemAmount[14]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts17", itemAmount[15]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts18", itemAmount[16]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts19", itemAmount[17]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts20", itemAmount[18]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts21", itemAmount[19]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts22 ", itemAmount[20]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts23", itemAmount[21]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts24", itemAmount[22]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts25", itemAmount[23]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts26", itemAmount[24]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts27", itemAmount[25]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts28 ", itemAmount[26]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts29", itemAmount[27]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts30", itemAmount[28]);
                insJrnCmd.Parameters.AddWithValue("@item_amounts31", itemAmount[29]);
                insJrnCmd.Parameters.AddWithValue("@username", "billing");
                insJrnCmd.Parameters.AddWithValue("@userdate", now.ToString("d")); //---
                insJrnCmd.Parameters.AddWithValue("@usertime", now.ToString("T").Substring(0, 8));
                insJrnCmd.Parameters.AddWithValue("@effective_date", lastOfThisMonth); //---
                insJrnCmd.Parameters.AddWithValue("@period ", ClsRefData.alPeriod);
                insJrnCmd.Parameters.AddWithValue("@slyear", ClsRefData.alYear);
                insJrnCmd.Parameters.AddWithValue("@control", control_acc);
                insJrnCmd.Parameters.AddWithValue("@posting_ind", "N");

                insJrnCmd.ExecuteNonQuery();
                for (int jdx = 0; jdx < 31; jdx++)
                {
                    itemAmount[jdx] = 0;
                    itemNumber[jdx] = "  ";
                }
                  
            }
            procReader.Close();
            procReader.Dispose();
            sqlCon.Close();
            MessageBox.Show("Processing competed. Go to Sage AR Journals and Post Batch number " + ClsRefData.alBatch.ToString());
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ClsRefData.yrMon();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Year  :  " + ClsRefData.alYear + "    Month   :  " + ClsRefData.alPeriod + "  Batch Number :  " + ClsRefData.alBatch);
            DateTime date = DateTime.Now;
      
            DateTime firstOfNextMonth = new DateTime(date.Year, date.Month, 1).AddMonths(1);
            DateTime lastOfThisMonth = firstOfNextMonth.AddDays(-1);

            //MessageBox.Show("First day    : " + firstOfNextMonth + "  Last day  : " + lastOfThisMonth);

        }

      

               


    }
}
