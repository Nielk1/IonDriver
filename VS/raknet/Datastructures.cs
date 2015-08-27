using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace raknet
{
    public class GameData
    {
        public string gameId;
        public string addr;
        public long clientReqId;
        public long timeoutSec;
        public string updatePw; //"PandemicRIP"
        public DateTime lastUpdate;
        public string rowPW;
        public long rowId;

        public Dictionary<string, JValue> customValues;

        public GameData()
        {
            customValues = new Dictionary<string, JValue>();
        }
    }
}