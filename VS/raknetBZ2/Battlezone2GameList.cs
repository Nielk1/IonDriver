using Newtonsoft.Json.Linq;
using raknet;
using System;
using System.Collections.Generic;
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
        public Version Version { get { return typeof(Battlezone2GameList).Assembly.GetName().Version; /*new Version(0, 0, 1, 0);*/ } }
        public string DisplayName { get { return Name + @" (" + Version.ToString() + @")"; } }

        public List<GameData> PreProcessGameList(List<GameData> rawGames)
        {
            {
                GameData hardCodedGame = new GameData()
                {
                    addr = @"0.0.0.0:0",
                    clientReqId = 0,
                    gameId = "BZ2",
                    lastUpdate = DateTime.UtcNow,
                    rowId = 0,
                    rowPW = string.Empty,
                    timeoutSec = 300,
                    //updatePw = string.Empty
                };
                hardCodedGame.customValues["n"] = new JValue(@"TestGame");
                hardCodedGame.customValues["m"] = new JValue(string.Empty);
                hardCodedGame.customValues["d"] = new JValue(string.Empty);
                hardCodedGame.customValues["k"] = new JValue(1);
                hardCodedGame.customValues["t"] = new JValue(7);
                hardCodedGame.customValues["r"] = new JValue(@"@ZA@d1");
                hardCodedGame.customValues["v"] = new JValue(@"S1");

                rawGames.Add(hardCodedGame);
            }

            WebRequest myWebRequest = WebRequest.Create(@"http://gamelist.kebbz.com/testServer?__gameId=BZ2");
            myWebRequest.Timeout = 1000; // 1 second

            try
            {
                using (WebResponse myWebResponse = myWebRequest.GetResponse())
                {
                    using (var reader = new StreamReader(myWebResponse.GetResponseStream()))
                    {
                        JObject kebbzData = JObject.Parse(reader.ReadToEnd());

                        ((JArray)(kebbzData["GET"])).Cast<JObject>().ToList().ForEach(dr =>
                        {
                            try
                            {
                                GameData kebbzGame = new GameData()
                                {
                                    addr = dr["__addr"].Value<string>(),
                                    clientReqId = dr["__clientReqId"].Value<long>(),
                                    gameId = dr["__gameId"].Value<string>(),
                                    lastUpdate = DateTime.UtcNow,
                                    rowId = 0,
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

                                rawGames.Add(kebbzGame);
                            }
                            catch { }
                        });
                    }
                }
            }
            catch { }

            return rawGames;
        }
    }
}