using Newtonsoft.Json.Linq;
using raknet;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;

namespace raknetBZ2
{
    public class Battlezone2GameList : IGameListPlugin
    {
        public string GameID { get { return "BZ2"; } }
        public string Name { get { return "Battlezone 2 GameList"; } }
        public Version Version { get { return typeof(Battlezone2GameList).Assembly.GetName().Version; } }
        public string DisplayName { get { return Name + @" (" + Version.ToString() + @")"; } }

        private object KebbzLock = new object();
        private JObject KebbzData;
        private DateTime KebbzDataDate;

        public void InterceptQueryStringForGet(ref NameValueCollection queryString) { }

        public void PreProcessGameList(NameValueCollection queryString, ref List<GameData> rawGames)
        {
            bool DoProxy = true;
            if (queryString["__pluginProxy"] != null && !bool.TryParse(queryString["__pluginProxy"], out DoProxy))
            {
                DoProxy = true;
            }

            bool ShowSource = false;
            if (queryString["__pluginShowSource"] != null && !bool.TryParse(queryString["__pluginShowSource"], out ShowSource))
            {
                ShowSource = false;
            }

            long rowIdCounter = 0;

            {
                GameData hardCodedGame = new GameData()
                {
                    addr = @"0.0.0.0:17771",
                    clientReqId = 0,
                    gameId = "BZ2",
                    lastUpdate = DateTime.UtcNow,
                    rowId = rowIdCounter--,
                    rowPW = string.Empty,
                    timeoutSec = 300,
                    //updatePw = string.Empty
                };
                hardCodedGame.customValues["n"] = new JValue(@"IonDriver - Bismuth");
                hardCodedGame.customValues["m"] = new JValue(@"bismuth");
                hardCodedGame.customValues["d"] = new JValue(string.Empty);
                hardCodedGame.customValues["k"] = new JValue(1);
                hardCodedGame.customValues["t"] = new JValue(7);
                hardCodedGame.customValues["r"] = new JValue(@"@ZA@d1");
                hardCodedGame.customValues["v"] = new JValue(@"S1");

                rawGames.Add(hardCodedGame);
            }

            if (DoProxy)
            {
                lock (KebbzLock)
                {
                    if (KebbzData == null || KebbzDataDate == null || KebbzDataDate.AddSeconds(10) < DateTime.UtcNow)
                    {
                        WebRequest myWebRequest = WebRequest.Create(@"http://gamelist.kebbz.com/testServer?__gameId=BZ2");
                        myWebRequest.Timeout = 1000; // 1 second

                        try
                        {
                            using (WebResponse myWebResponse = myWebRequest.GetResponse())
                            {
                                using (var reader = new StreamReader(myWebResponse.GetResponseStream()))
                                {
                                    KebbzData = JObject.Parse(reader.ReadToEnd());
                                    KebbzDataDate = DateTime.UtcNow;
                                }
                            }
                        }
                        catch
                        {
                            KebbzData = null;
                            KebbzDataDate = DateTime.UtcNow;
                        }
                    }

                    if (KebbzData != null)
                    {
                        rawGames.AddRange(((JArray)(KebbzData["GET"])).Cast<JObject>().ToList().Select(dr =>
                        {
                            try
                            {
                                GameData kebbzGame = new GameData()
                                {
                                    addr = dr["__addr"].Value<string>(),
                                    clientReqId = dr["__clientReqId"].Value<long>(),
                                    gameId = dr["__gameId"].Value<string>(),
                                    lastUpdate = DateTime.UtcNow,
                                    rowId = rowIdCounter--,
                                    rowPW = string.Empty,
                                    timeoutSec = dr["__timeoutSec"].Value<long>(),
                                    //updatePw = string.Empty
                                };

                                dr.Properties().ToList().ForEach(dx =>
                                {
                                    if (!dx.Name.StartsWith("__"))
                                    {
                                        kebbzGame.customValues[dx.Name] = (JValue)dx.Value;
                                    }
                                });

                                if(ShowSource)
                                    kebbzGame.customValues["proxySource"] = new JValue("gamelist.kebbz.com");

                                //rawGames.Add(kebbzGame);
                                return kebbzGame;
                            }
                            catch { }
                            return null;
                        }).Where(dr => dr != null));
                    }
                }
            }

            //return rawGames;
        }
    }
}