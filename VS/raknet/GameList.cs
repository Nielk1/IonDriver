using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace raknet
{
    /// <summary>
    /// Interfaces with game list data storage database
    /// </summary>
    public class GameList
    {
        //private static readonly Lazy<GameList> lazy = new Lazy<GameList>(() => new GameList());
        //public static GameList Instance { get { return lazy.Value; } }

        Global global;

        public GameList(Global global)
        {
            this.global = global;
        }

        public void CleanStaleGames()
        {
            global.SingleThreadDatabaseInterface.CleanStaleGames();
        }
        
        public List<GameData> GetGames(string __gameId)
        {
            return global.SingleThreadDatabaseInterface.GetGames(__gameId);
        }

        public long AddGame(string gameId, DateTime lastUpdate, int timeoutSec, string rowPW, long clientReqId, string iP, Dictionary<string, JValue> customValues)
        {
            return global.SingleThreadDatabaseInterface.AddGame(gameId, lastUpdate, timeoutSec, rowPW, clientReqId, iP, customValues);
        }

        public void DeleteGame(long rowId)
        {
            global.SingleThreadDatabaseInterface.DeleteGame(rowId);
        }

        public void UpdateGame(long rowId, DateTime lastUpdate, int timeoutSec, long clientReqId, string iP, Dictionary<string, JValue> customValues)
        {
            global.SingleThreadDatabaseInterface.UpdateGame(rowId, lastUpdate, timeoutSec, clientReqId, iP, customValues);
        }

        public void CheckGame(string iP, long clientReqId, out long qryRowId, out string qryRowPW)
        {
            global.SingleThreadDatabaseInterface.CheckGame(iP, clientReqId, out qryRowId, out qryRowPW);
        }

        public void CheckGame(long rowId, out string qryRowPW)
        {
            global.SingleThreadDatabaseInterface.CheckGame(rowId, out qryRowPW);
        }
    }
}