using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using System.Text.RegularExpressions;
//using System.Net.WebClient;


namespace GetMediaStatistic
{
    class Program
    {

        // Declaring Class
        private class Accounts
        {
            public int OTT;
            public int CTI;
            public int STB;
            public int b2b;
            public int others;

            public int All()
            {
                return OTT + CTI + STB + b2b + others;
            }
        } // stop Class Declaring

        static void Main(string[] args)
        {
            Log("Start Program");
            Accounts accounts = new Accounts();

            Log("Select from BranchDB");
            // accounts = GetAccountsInBranch();

            string web_activeMobileLogings;
            String[] activeMobileLogings;

            using (var client = new System.Net.WebClient())
            {
                //client.Credentials = new NetworkCredential("user", "password");
                web_activeMobileLogings = client.DownloadString("http://dior.corbina.net/active_mobile_logins.txt");
                //Console.WriteLine(activeMobileLogings);

                web_activeMobileLogings = Regex.Replace(web_activeMobileLogings, @"\s9", @"79").TrimEnd();
                String pattern = @"\n";
                
                activeMobileLogings = Regex.Split(web_activeMobileLogings, pattern);

                foreach (var activeMobileLogin in activeMobileLogings)
                {
                   
                    Console.WriteLine(activeMobileLogin);
                }
            }

            /*
            Log(String.Format("OTT :{0}, STB :{1}, CTI :{2}, b2b :{3}, others :{4}, ALL :{5}",
                    accounts.OTT, accounts.STB, accounts.CTI, accounts.b2b, accounts.others,
                    accounts.All()));


            Console.WriteLine("OTT :{0}, STB :{1}, CTI :{2}, b2b :{3}, others :{4}, ALL :{5}",
                    accounts.OTT, accounts.STB, accounts.CTI, accounts.b2b, accounts.others,
                    accounts.All());
            */

            /* stop for a monent 
            
            //check sqlite file
            string path = Properties.Settings.Default.TMPPath;
            string filename = "\\MediaroomStatistic.sqlite";
                        
            string sqlfile = path + filename;
            if (!File.Exists(sqlfile))
            {
                Console.WriteLine("Create SQLlite file DB");
                Log("Create SQLlite file DB");
                CreateSQLiteDB(sqlfile);
            }
            
            Log("Add information to SQLite DB");
            UpdateSQLiteDB(accounts, sqlfile);

            Log("Copy file to web server");
            string uncpath = Properties.Settings.Default.DataPath;

            try {
                File.Copy(sqlfile, uncpath + filename, true);
            } catch (Exception e)
            {
                Console.WriteLine("Error with copy file to uncpath" + e.ToString());
                Log("Error with copy file to uncpath" + e.ToString());
            } 
           
            */

            Console.ReadLine();
            Log("Stop Program");

        }// main


        static void UpdateSQLiteDB(Accounts accounts, string sqlfile)
        {
            try
            {
                using (SQLiteConnection sqlite = new SQLiteConnection())
                {
                    //sqlfile = "\\mskrusmdsif004\c$\inetpub\wwwroot\ShowMediaroomStatistic\App_Data\MediaroomStatistic.db";
                    sqlite.ConnectionString = "Data Source=" + sqlfile;
                    sqlite.Open();

                    string query = "REPLACE into MR_statistics (Name, Value, UpdateDate) VALUES " +
                                   "('OTT_accounts','" + accounts.OTT.ToString() + "', DateTime('now') )," +
                                   "('CTI_accounts','" + accounts.CTI.ToString() + "', DateTime('now') )," +
                                   "('b2b_accounts','" + accounts.b2b.ToString() + "', DateTime('now') )," +
                                   "('Others_accounts', '" + accounts.others.ToString() + "', DateTime('now') )," +
                                   "('ALL_accounts', '" + accounts.All().ToString() + "', DateTime('now') ), " +
                                   "('STB_accounts','" + accounts.STB.ToString() + "', DateTime('now') );";

                    SQLiteCommand command = new SQLiteCommand(query, sqlite);
                    command.ExecuteNonQuery();
                    

                }

            } catch (Exception e)
            {
                Console.WriteLine("Error with update data in SQLite" + e.ToString());
                Log("Error with update data in SQLite" + e.ToString());
            }
        }

        static void CreateSQLiteDB(string sqlfile)
        {
            try
            {
                SQLiteConnection.CreateFile(sqlfile);

                using (SQLiteConnection sqlite = new SQLiteConnection())
                {
                    sqlite.ConnectionString = "Data Source=" + sqlfile;
                    sqlite.Open();

                    string query = @"CREATE TABLE MR_statistics (Name VARCHAR(20) UNIQUE, Value VARCHAR(100),UpdateDate DATETIME)";
                    SQLiteCommand command = new SQLiteCommand(query, sqlite);
                    command.ExecuteNonQuery();
                }
            } catch (Exception e)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error with SQLite " + e.ToString());
                Log("Error with SQLite" + e.ToString());
                //Console.ResetColor();                
            }


        }


        static Accounts GetAccountsInBranch()
        {
            Accounts accounts = new Accounts();
            try
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = "server=MSKRUSBRDBCS1\\BRDB;database=BranchDB;trusted_connection=true;";
                    conn.Open();

                    string query = @"SELECT SUBSTRING( [externalId] ,0, 3 ) as typeacc, count(*) as num
                                 FROM[BranchDB].[dbo].[bm_account] with(nolock)
                                 group by SUBSTRING([externalId] ,0, 3 )
                                 order by num desc";


                    SqlCommand command = new SqlCommand(query, conn);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //Console.WriteLine( string.Format("{0} \t | {1} ",reader[0],reader[1]) );
                            switch (reader[0].ToString())
                            {
                                case "79":
                                    accounts.OTT = Convert.ToInt32(reader[1]);
                                    break;

                                case "CO":
                                case "Co":
                                    accounts.STB = Convert.ToInt32(reader[1]);
                                    break;

                                case "CT":
                                    accounts.CTI = Convert.ToInt32(reader[1]);
                                    break;

                                case "b2":
                                    accounts.b2b = Convert.ToInt32(reader[1]);
                                    break;

                                default:
                                    accounts.others = accounts.others + Convert.ToInt32(reader[1]);
                                    break;
                            }// switch

                        }
                    } //using              
                } // using
            }
            catch (Exception e)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error with BranchDB " + e.ToString());
                //Console.ResetColor();                
            }
            return accounts;
        } // 

        public static void Log(string logMessage)
        {
            //string path = Properties.Settings.Default.LogPath;
            string path = ".\\";


            string filename = Process.GetCurrentProcess().ProcessName + "_" + DateTime.Today.ToString("yyyy-MM-dd") + ".log";

            try
            {
                using (StreamWriter w = File.AppendText(path + "\\" + filename))
                {
                    w.Write("{0}\t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    w.WriteLine(logMessage);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error to write file log " + e.ToString());
            }

        }// end of log
    }
}
