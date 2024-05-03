using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseUtils
{
    class Program
    {
        static void Main(string[] args)
        {

            var dbPath = @"D:\Workspace\swipe-desktop\VisitorHub\App_Data\SwipeDesktop.mdf";

              String strConnection = @"Data Source=.\SQLEXPRESS;
                          AttachDbFilename=" + dbPath + ";" +
                          "Integrated Security=True;" +
                          "Connect Timeout=30;" +
                          "User Instance=False";

            SqlConnection con = new SqlConnection(strConnection);
            try
            {

                con.Open();

                SqlCommand sqlCmd = new SqlCommand();

                sqlCmd.Connection = con;
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.CommandText = "SELECT * FROM INFORMATION_SCHEMA.TABLES";

                SqlDataAdapter sqlDataAdap = new SqlDataAdapter(sqlCmd);

                DataTable dtRecord = new DataTable();
                sqlDataAdap.Fill(dtRecord);
               
                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error");
            }
        }
    
    }
}
