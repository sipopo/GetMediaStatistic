using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using MSUtil;


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


        private class MR_statistic
        {
            public string Name;
            public string Value;
        }

        
        private class Http_codes
        {
            public int code_2xx;
            public int code_5xx;
            public int code_others;
        }
        

        static void Main(string[] args)
        {

            List<MR_statistic> MR_statistics = new List<MR_statistic>();

            Log("Start Program");          

            Log("Select from BranchDB");
            MR_statistics.AddRange(GetAccountsInBranch());

            Log("Select from Synchrobe");
            MR_statistics.AddRange(GetSynchrobeStat());

            Log("Statistics information:");
            foreach (var MR_statistic in MR_statistics)
            {
                Console.WriteLine(MR_statistic.Name + ":" + MR_statistic.Value);                
                Log(String.Format(" {0}: {1} ", MR_statistic.Name,MR_statistic.Value));
            }

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
            UpdateSQLiteDB(MR_statistics, sqlfile);

            
            Log("Copy file to web server");
            string uncpath = Properties.Settings.Default.DataPath;

            try
            {
                File.Copy(sqlfile, uncpath + filename, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with copy file to uncpath" + e.ToString());
                Log("Error with copy file to uncpath" + e.ToString());
            }
            

            //Console.ReadLine();
            Log("Stop Program");

        }// main


        static List<MR_statistic> GetSynchrobeStat()
        {
            Http_codes Http_codes = new Http_codes();

            //string[] servernames = { "MSKRUSSTBSYN001", "MSKRUSSTBSYN002" };
            //string[] times = { "00:15:00", "00:30:00", "01:00:00" };
            Dictionary<string,string> servers = new Dictionary<string, string>();
            servers.Add("synh01", "MSKRUSSTBSYN001");
            servers.Add("synh02", "MSKRUSSTBSYN002");

            Dictionary<string, string> times = new Dictionary<string, string>();
            times.Add("15m", "00:15:00");
            times.Add("30m", "00:30:00");
            times.Add("1h", "01:00:00");

            string LogPath = @"\c$\inetpub\logs\LogFiles\W3SVC3";            
            string FileName = "u_ex" + DateTime.UtcNow.ToString("yyMMdd") + "*.log";
            string Filename2 = "u_ex" + DateTime.UtcNow.AddDays(-1).ToString("yyMMdd") + "*.log";

            Log("DEBUG: utc time "+ DateTime.UtcNow.ToString("yyMMdd") + " current time - " + DateTime.Now.ToString("yyMMdd HH:mm:ss"));

            LogQueryClass LogParser = null;
            COMIISW3CInputContextClass IISLog = null;            

            LogParser = new LogQueryClass();
            IISLog = new COMIISW3CInputContextClass();

            List<MR_statistic> MR_statistics = new List<MR_statistic>();
            string strSQL = null;
            try
            {
                foreach (var server in servers)
                { 
                    foreach (var time in times)                   
                    {

                       
                        strSQL =  " SELECT MAX(time-taken) as MAX_tt, MIN(time-taken) as MIN_tt, AVG(time-taken) as AVG_tt " +
                                      " FROM \\\\" + server.Value + "\\" + LogPath + "\\" + FileName +
                                      " ,\\\\" + server.Value + "\\" + LogPath + "\\" + Filename2 +
                                      " WHERE TO_TIMESTAMP(date, time) > SUB( SYSTEM_TIMESTAMP(), TO_TIMESTAMP('" + time.Value + "','HH:mm:ss') )  ";

                        Log("DEBUG: sql " + strSQL);
                        //Console.WriteLine(strSQL);
                        // run query
                        ILogRecordset rsLP = LogParser.Execute(strSQL, IISLog);
                        /*
                        Console.WriteLine(rsLP.getRecord().getValue("MAX_tt"));
                        Console.WriteLine(rsLP.getRecord().getValue("MIN_tt"));
                        Console.WriteLine(rsLP.getRecord().getValue("AVG_tt"));
                        */                        
                        MR_statistic MR_statistic = new MR_statistic();
                        MR_statistic.Name = server.Key + "_" + time.Key + "_" + "MaxTT";
                        MR_statistic.Value = Convert.ToString(rsLP.getRecord().getValue("MAX_tt"));
                        MR_statistics.Add(MR_statistic);   

                        MR_statistic = new MR_statistic();
                        MR_statistic.Name = server.Key + "_" + time.Key + "_" + "MinTT";
                        MR_statistic.Value = Convert.ToString(rsLP.getRecord().getValue("MIN_tt"));
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Name = server.Key + "_" + time.Key + "_" + "AvgTT";
                        MR_statistic.Value = Convert.ToString(rsLP.getRecord().getValue("AVG_tt"));
                        MR_statistics.Add(MR_statistic);

                        // for debug     
                                       
                        strSQL = " SELECT TOP 1 SYSTEM_TIMESTAMP() as system_time ,TO_TIMESTAMP( date,time) as iis_time, TO_LOCALTIME(TO_TIMESTAMP(date,time)) as local_iistime " +
                                 ", SUB(SYSTEM_TIMESTAMP(), TO_TIMESTAMP('" + time.Value + "', 'HH:mm:ss')) as where_time" +
                                  " FROM \\\\" + server.Value + "\\" + LogPath + "\\" + FileName +
                                  " ,\\\\" + server.Value + "\\" + LogPath + "\\" + Filename2 +
                                  " WHERE TO_TIMESTAMP(date, time) > SUB( SYSTEM_TIMESTAMP(), TO_TIMESTAMP('" + time.Value + "','HH:mm:ss') )  ";

                        Log("DEBUG: sql - " + strSQL);
                        rsLP = LogParser.Execute(strSQL, IISLog);
                       
                        Log("DEBUG : system_time " + rsLP.getRecord().getValue("system_time") + 
                            " iis_time - " + rsLP.getRecord().getValue("iis_time") +
                            " local_iistime - " + rsLP.getRecord().getValue("local_iistime") +
                            " where_time - " + rsLP.getRecord().getValue("where_time")
                            );
                           
                        // for debug - end

                        Http_codes = new Http_codes();

                        strSQL = " SELECT sc-status, count(*) as num" +
                                 " FROM \\\\" + server.Value + "\\" + LogPath + "\\" + FileName +
                                 " ,\\\\" + server.Value + "\\" + LogPath + "\\" + Filename2 +
                                 " WHERE TO_TIMESTAMP(date, time) > SUB( SYSTEM_TIMESTAMP(), TO_TIMESTAMP('" + time.Value + "','HH:mm:ss') )  " +
                                 " GROUP BY  sc-status ";

                        Log("DEBUG: sql " + strSQL);
                        rsLP = LogParser.Execute(strSQL, IISLog);                        
                        while (!rsLP.atEnd())
                        {
                            string sw = Convert.ToString(rsLP.getRecord().getValue("sc-status"));                          
                            switch (sw[0])
                            {
                                case '2':
                                    Http_codes.code_2xx = Http_codes.code_2xx + rsLP.getRecord().getValue("num");
                                    break;

                                case '5':
                                    Http_codes.code_5xx = Http_codes.code_5xx + rsLP.getRecord().getValue("num");
                                    break;

                                default:
                                    Http_codes.code_others = Http_codes.code_others + rsLP.getRecord().getValue("num");
                                    break;
                            }// switch
                            rsLP.moveNext();
                        }

                        MR_statistic = new MR_statistic();
                        MR_statistic.Name = server.Key + "_" + time.Key + "_" + "HTTP-2xx";
                        MR_statistic.Value = Convert.ToString(Http_codes.code_2xx);
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Name = server.Key + "_" + time.Key + "_" + "HTTP-5xx";
                        MR_statistic.Value = Convert.ToString(Http_codes.code_5xx);
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Name = server.Key + "_" + time.Key + "_" + "HTTP-others";
                        MR_statistic.Value = Convert.ToString(Http_codes.code_5xx);
                        MR_statistics.Add(MR_statistic);

                    }
                    //Console.WriteLine("---");
                }// foreach                              
            }
            catch (Exception ex)
            {
                Console.WriteLine("Query string: {0}", strSQL);
                Console.WriteLine("Something wrong: {0}", ex.Message);
                Log("Something wrong: " + ex.Message);
               // SendFlag = 0;

            }
            return MR_statistics;
        }// End of GetSynchrobeStat

        static void UpdateSQLiteDB(List<MR_statistic> MR_Statistics, string sqlfile)
        {
            try
            {
                using (SQLiteConnection sqlite = new SQLiteConnection())
                {
                    //sqlfile = "\\mskrusmdsif004\c$\inetpub\wwwroot\ShowMediaroomStatistic\App_Data\MediaroomStatistic.db";
                    sqlite.ConnectionString = "Data Source=" + sqlfile;
                    sqlite.Open();
                    /*
                    string query = "REPLACE into MR_statistics (Name, Value, UpdateDate) VALUES " +
                                   "('OTT_accounts','" + accounts.OTT.ToString() + "', DateTime('now') )," +
                                   "('CTI_accounts','" + accounts.CTI.ToString() + "', DateTime('now') )," +
                                   "('b2b_accounts','" + accounts.b2b.ToString() + "', DateTime('now') )," +
                                   "('Others_accounts', '" + accounts.others.ToString() + "', DateTime('now') )," +
                                   "('ALL_accounts', '" + accounts.All().ToString() + "', DateTime('now') ), " +
                                   "('STB_accounts','" + accounts.STB.ToString() + "', DateTime('now') );";
                    */
                    foreach (var MR_statistic in MR_Statistics)
                    {
                        string query = "REPLACE into MR_statistics (Name, Value, UpdateDate) VALUES " +
                                       "('"+ MR_statistic.Name + "','" + MR_statistic.Value + "', DateTime('now') );";

                        SQLiteCommand command = new SQLiteCommand(query, sqlite);
                        command.ExecuteNonQuery();
                    }                                                            
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


        static List<MR_statistic> GetAccountsInBranch()
        {
            Accounts accounts = new Accounts();
            List <MR_statistic> MR_statistics = new List<MR_statistic>();

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
                        MR_statistic MR_statistic = new MR_statistic();
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
                        MR_statistic = new MR_statistic();
                        MR_statistic.Value = Convert.ToString(accounts.others);
                        MR_statistic.Name = "Others_accounts";
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Value = Convert.ToString(accounts.All().ToString());
                        MR_statistic.Name = "ALL_accounts";
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Value = Convert.ToString(accounts.b2b);
                        MR_statistic.Name = "b2b_accounts";
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Value = Convert.ToString(accounts.CTI);
                        MR_statistic.Name = "CTI_accounts";
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Value = Convert.ToString(accounts.STB);
                        MR_statistic.Name = "STB_accounts";
                        MR_statistics.Add(MR_statistic);

                        MR_statistic = new MR_statistic();
                        MR_statistic.Value = Convert.ToString(accounts.OTT);
                        MR_statistic.Name = "OTT_accounts";
                        MR_statistics.Add(MR_statistic);
                    } //using              
                } // using
            }
            catch (Exception e)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error with BranchDB " + e.ToString());
                //Console.ResetColor();                
            }
            return MR_statistics;
        } // 

        public static void Log(string logMessage)
        {
            string path = Properties.Settings.Default.LogPath;
            //string path = ".\\";


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
