using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;

namespace raknet
{
    public class SqliteInterface
    {
        //private static readonly Lazy<GameList> lazy = new Lazy<GameList>(() => new GameList());
        //public static GameList Instance { get { return lazy.Value; } }

        private static SqliteInterface instance;
        public static SqliteInterface Instance
        {
            get
            {
                if (instance == null)
                    instance = new SqliteInterface();
                return instance;
            }
        }

        private SQLiteConnection cnn;

        private System.Object FileInteractionLock = new System.Object();

        private SqliteInterface()
        {
            DbProviderFactory fact = DbProviderFactories.GetFactory("System.Data.SQLite");

            cnn = (SQLiteConnection)fact.CreateConnection();//using (DbConnection cnn = fact.CreateConnection())
            {
                cnn.ConnectionString = ConfigurationManager.ConnectionStrings["RaknetDB"].ConnectionString;
                cnn.Open();
            }
        }

        ~SqliteInterface()
        {
            //cnn.Close();
        }

        public void CleanStaleGames()
        {
            lock (FileInteractionLock)//using (SQLiteConnection cnn = GetConnection())
            {
                using (SQLiteTransaction mytransaction = cnn.BeginTransaction())
                {
                    using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                    {
                        SQLiteParameter myparam = new SQLiteParameter();
                        mycommand.CommandText = "DELETE FROM gamelist WHERE datetime(lastUpdate,'+'||timeoutSec||' seconds') <= ?";
                        mycommand.Parameters.Add(myparam);
                        myparam.Value = DateTime.UtcNow;
                        mycommand.ExecuteNonQuery();
                    }
                    using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                    {
                        mycommand.CommandText = "DELETE FROM gameattr WHERE NOT EXISTS (SELECT rowId FROM gamelist WHERE rowId = gameRowId)";
                        mycommand.ExecuteNonQuery();
                    }
                    mytransaction.Commit();
                }
            }
        }

        public List<GameData> GetGames(string __gameId)
        {
            List<GameData> games = new List<GameData>();

            lock (FileInteractionLock)//using (SQLiteConnection cnn = GetConnection())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter myparam = new SQLiteParameter();

                    mycommand.CommandText = "SELECT rowId,lastUpdate,timeoutSec,rowPW,clientReqId,addr FROM gamelist WHERE gameId = ?";
                    mycommand.Parameters.Add(myparam);

                    myparam.Value = __gameId;
                    using (SQLiteDataReader reader = mycommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                games.Add(new GameData()
                                {
                                    rowId = (long)reader["rowId"],
                                    lastUpdate = (DateTime)reader["lastUpdate"],
                                    timeoutSec = (long)reader["timeoutSec"],
                                    rowPW = (string)reader["rowPW"],
                                    clientReqId = (long)reader["clientReqId"],
                                    addr = (string)reader["addr"],
                                    gameId = __gameId
                                });
                            }
                        }
                    }
                }
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter myparam = new SQLiteParameter();

                    mycommand.CommandText = "SELECT rowID,key,type,string,integer FROM gameattr WHERE gameRowId = ?";
                    mycommand.Parameters.Add(myparam);

                    games.ForEach(dr =>
                    {
                        myparam.Value = dr.rowId;
                        using (SQLiteDataReader reader = mycommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    switch ((long)reader["type"])
                                    {
                                        case 0:
                                            dr.customValues[(string)reader["key"]] = new JValue((string)reader["string"]);
                                            break;
                                        case 1:
                                            dr.customValues[(string)reader["key"]] = new JValue((long)reader["integer"]);
                                            break;
                                    }
                                }
                            }
                        }
                    });

                }
            }

            return games;
        }

        public long AddGame(string gameId, DateTime lastUpdate, int timeoutSec, string rowPW, long clientReqId, string iP, Dictionary<string, JValue> customValues)
        {
            lock (FileInteractionLock)//using (SQLiteConnection cnn = GetConnection())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter myparam1 = new SQLiteParameter();
                    SQLiteParameter myparam2 = new SQLiteParameter();
                    SQLiteParameter myparam3 = new SQLiteParameter();
                    SQLiteParameter myparam4 = new SQLiteParameter();
                    SQLiteParameter myparam5 = new SQLiteParameter();
                    SQLiteParameter myparam6 = new SQLiteParameter();

                    mycommand.CommandText = "INSERT INTO gamelist (gameId,lastUpdate,timeoutSec,rowPW,clientReqId,addr) VALUES (?,?,?,?,?,?)";
                    mycommand.Parameters.Add(myparam1);
                    mycommand.Parameters.Add(myparam2);
                    mycommand.Parameters.Add(myparam3);
                    mycommand.Parameters.Add(myparam4);
                    mycommand.Parameters.Add(myparam5);
                    mycommand.Parameters.Add(myparam6);


                    myparam1.Value = gameId;
                    myparam2.Value = lastUpdate;
                    myparam3.Value = timeoutSec;
                    myparam4.Value = rowPW;
                    myparam5.Value = clientReqId;
                    myparam6.Value = iP;
                    mycommand.ExecuteNonQuery();
                }

                long gameRow = -1;

                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    mycommand.CommandText = "SELECT last_insert_rowid()";
                    gameRow = (long)(mycommand.ExecuteScalar());
                }

                using (SQLiteTransaction mytransaction = cnn.BeginTransaction())
                {
                    /*using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                    {
                        SQLiteParameter myparam = new SQLiteParameter();
                        mycommand.CommandText = "DELETE FROM gameattr WHERE rowID = ?";
                        mycommand.Parameters.Add(myparam);
                        myparam.Value = gameRow;
                        mycommand.ExecuteNonQuery();
                    }*/
                    using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                    {
                        SQLiteParameter myparam1 = new SQLiteParameter();
                        SQLiteParameter myparam2 = new SQLiteParameter();
                        SQLiteParameter myparam3 = new SQLiteParameter();
                        SQLiteParameter myparam4 = new SQLiteParameter();
                        SQLiteParameter myparam5 = new SQLiteParameter();

                        mycommand.CommandText = "INSERT INTO gameattr (gameRowId,key,type,string,integer) VALUES (?,?,?,?,?)";
                        mycommand.Parameters.Add(myparam1);
                        mycommand.Parameters.Add(myparam2);
                        mycommand.Parameters.Add(myparam3);
                        mycommand.Parameters.Add(myparam4);
                        mycommand.Parameters.Add(myparam5);

                        myparam1.Value = gameRow;
                        customValues.ToList().ForEach(dr =>
                        {
                            myparam2.Value = dr.Key;

                            myparam4.Value = null;
                            myparam5.Value = null;

                            switch (dr.Value.Type)
                            {
                                case JTokenType.String:
                                    {
                                        myparam3.Value = 0;
                                        myparam4.Value = dr.Value.Value<string>();
                                        myparam5.Value = null;
                                    }
                                    break;
                                case JTokenType.Integer:
                                    {
                                        myparam3.Value = 1;
                                        myparam4.Value = null;
                                        myparam5.Value = dr.Value.Value<int>();
                                    }
                                    break;
                            }

                            mycommand.ExecuteNonQuery();
                        });
                    }
                    mytransaction.Commit();
                }

                return gameRow;
            }
        }

        public void DeleteGame(long rowId)
        {
            lock (FileInteractionLock)//using (SQLiteConnection cnn = GetConnection())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter myparam = new SQLiteParameter();
                    mycommand.CommandText = "DELETE FROM gamelist WHERE rowId = ?";
                    mycommand.Parameters.Add(myparam);
                    myparam.Value = rowId;
                    mycommand.ExecuteNonQuery();
                }
            }
        }

        public void UpdateGame(long rowId, DateTime lastUpdate, int timeoutSec, long clientReqId, string iP, Dictionary<string, JValue> customValues)
        {
            lock (FileInteractionLock)//using (SQLiteConnection cnn = GetConnection())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter myparam1 = new SQLiteParameter();
                    SQLiteParameter myparam2 = new SQLiteParameter();
                    SQLiteParameter myparam3 = new SQLiteParameter();
                    SQLiteParameter myparam4 = new SQLiteParameter();
                    SQLiteParameter myparam5 = new SQLiteParameter();

                    mycommand.CommandText = "UPDATE gamelist SET lastUpdate = ?,timeoutSec = ?,clientReqId = ?,addr = ? WHERE rowID = ?";
                    mycommand.Parameters.Add(myparam1);
                    mycommand.Parameters.Add(myparam2);
                    mycommand.Parameters.Add(myparam3);
                    mycommand.Parameters.Add(myparam4);
                    mycommand.Parameters.Add(myparam5);

                    myparam1.Value = lastUpdate;
                    myparam2.Value = timeoutSec;
                    myparam3.Value = clientReqId;
                    myparam4.Value = iP;
                    myparam5.Value = rowId;

                    mycommand.ExecuteNonQuery();
                }

                long gameRow = rowId;

                //using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                //{
                //    mycommand.CommandText = "SELECT last_insert_rowid()";
                //    gameRow = (long)(mycommand.ExecuteScalar());
                //}

                using (SQLiteTransaction mytransaction = cnn.BeginTransaction())
                {
                    using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                    {
                        SQLiteParameter myparam = new SQLiteParameter();
                        mycommand.CommandText = "DELETE FROM gameattr WHERE gameRowId = ?";
                        mycommand.Parameters.Add(myparam);
                        myparam.Value = gameRow;
                        mycommand.ExecuteNonQuery();
                    }
                    using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                    {
                        SQLiteParameter myparam1 = new SQLiteParameter();
                        SQLiteParameter myparam2 = new SQLiteParameter();
                        SQLiteParameter myparam3 = new SQLiteParameter();
                        SQLiteParameter myparam4 = new SQLiteParameter();
                        SQLiteParameter myparam5 = new SQLiteParameter();

                        mycommand.CommandText = "INSERT INTO gameattr (gameRowId,key,type,string,integer) VALUES (?,?,?,?,?)";
                        mycommand.Parameters.Add(myparam1);
                        mycommand.Parameters.Add(myparam2);
                        mycommand.Parameters.Add(myparam3);
                        mycommand.Parameters.Add(myparam4);
                        mycommand.Parameters.Add(myparam5);

                        myparam1.Value = gameRow;
                        customValues.ToList().ForEach(dr =>
                        {
                            myparam2.Value = dr.Key;

                            myparam4.Value = null;
                            myparam5.Value = null;

                            switch (dr.Value.Type)
                            {
                                case JTokenType.String:
                                    {
                                        myparam3.Value = 0;
                                        myparam4.Value = dr.Value.Value<string>();
                                        myparam5.Value = null;
                                    }
                                    break;
                                case JTokenType.Integer:
                                    {
                                        myparam3.Value = 1;
                                        myparam4.Value = null;
                                        myparam5.Value = dr.Value.Value<int>();
                                    }
                                    break;
                            }

                            mycommand.ExecuteNonQuery();
                        });
                    }
                    mytransaction.Commit();
                }

                //return gameRow;
            }
        }

        public void CheckGame(string iP, long? clientReqId, out long qryRowId, out string qryRowPW)
        {
            lock (FileInteractionLock)//using (SQLiteConnection cnn = GetConnection())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter myparam1 = new SQLiteParameter();
                    SQLiteParameter myparam2 = new SQLiteParameter();

                    mycommand.CommandText = "SELECT rowId, rowPW FROM gamelist WHERE addr =? AND (? IS NULL OR clientReqId =?)";
                    mycommand.Parameters.Add(myparam1);
                    mycommand.Parameters.Add(myparam2);
                    mycommand.Parameters.Add(myparam2);


                    myparam1.Value = iP;
                    if (clientReqId.HasValue)
                    {
                        myparam2.Value = clientReqId.Value;
                    }
                    else
                    {
                        myparam2.Value = null;
                    }
                    SQLiteDataReader reader = mycommand.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        qryRowId = -1;
                        qryRowPW = string.Empty;
                        return;
                    }

                    reader.Read();
                    qryRowId = (long)reader["rowId"];
                    qryRowPW = (string)reader["rowPW"];
                }
            }
        }

        public void CheckGame(long rowId, out long qryRowId, out string qryRowPW)
        {
            lock (FileInteractionLock)//using (SQLiteConnection cnn = GetConnection())
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(cnn))
                {
                    SQLiteParameter myparam = new SQLiteParameter();
                    mycommand.CommandText = "SELECT rowPW FROM gamelist WHERE rowId = ?";
                    mycommand.Parameters.Add(myparam);
                    myparam.Value = rowId;
                    object resp = mycommand.ExecuteScalar();

                    if(resp != null)
                    {
                        qryRowPW = (string)resp;
                        qryRowId = rowId;
                    }
                    else
                    {
                        qryRowPW = null;
                        qryRowId = -1;
                    }
                }
            }
        }
    }
}