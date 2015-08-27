using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace raknet
{
    /// <summary>
    /// Summary description for testServer
    /// </summary>
    public class testServer : IHttpHandler
    {
        GameList gamelist;

        public void ProcessRequest(HttpContext context)
        {
            // as all we get from the context is the Global instance,
            // which doesn't change, we can safely re-use an existing
            // instance thanks to IsReusable being true
            if (gamelist == null)
            {
                gamelist = new GameList((Global)(context.ApplicationInstance));
            }

            switch (context.Request.RequestType)
            {
                case "GET":
                    GetGames(context);
                    break;
                case "POST":
                case "PUT":
                    PostGame(context);
                    break;
                case "DELETE":
                    DeleteGame(context);
                    break;
                default:
                    context.Response.StatusCode = 405; // Method Not Allowed
                    context.Response.Headers.Add("Allow", "GET, POST, PUT, DELETE");
                    break;
            }
        }

        private void GetGames(HttpContext context)
        {
            gamelist.CleanStaleGames();

            string __gameId = context.Request.QueryString["__gameId"];
            if(__gameId == null || __gameId.Length == 0)
            {
                context.Response.StatusCode = 400; // Bad Request
                return;
            }

            string geoIP = context.Request.QueryString["__geoIP"];
            if (geoIP == null || geoIP.Length == 0)
            {
                geoIP = context.Request.UserHostAddress;
            }

            List<string> excludedColumns = new List<string>();
            string strExcludedCols = context.Request.QueryString["__excludeCols"];
            if(strExcludedCols != null && strExcludedCols.Length > 0)
            {
                excludedColumns.AddRange(strExcludedCols.Split(','));
            }

            JObject responseObject = new JObject();

            JArray GameArray = new JArray();
            responseObject["GET"] = GameArray;

            gamelist.GetGames(__gameId).ForEach(dr =>
            {
                JObject obj = new JObject();
                if (!excludedColumns.Contains("__gameId")) obj["__gameId"] = dr.gameId;
                if (!excludedColumns.Contains("__rowId")) obj["__rowId"] = dr.rowId;
                //if (!excludedColumns.Contains("__updatePW")) obj["__updatePW"] = dr.updatePw;
                if (!excludedColumns.Contains("__addr")) obj["__addr"] = dr.addr;
                //if (!excludedColumns.Contains("__clientReqId")) obj["__clientReqId"] = dr.clientReqId;
                if (!excludedColumns.Contains("__timeoutSec")) obj["__timeoutSec"] = dr.timeoutSec;

                foreach (KeyValuePair<string, JValue> pair in dr.customValues)
                {
                    if (!excludedColumns.Contains(pair.Key))
                        obj[pair.Key] = pair.Value;
                }

                GameArray.Add(obj);
            });

            // this closure needs to be moved a decorator module
            {
                JObject obj = new JObject();
                obj["__gameId"] = @"BZ2";
                obj["__addr"] = @"0.0.0.0:0";
                obj["__clientReqId"] = 0;
                obj["__timeoutSec"] = 300;
                obj["n"] = @"TestGame";
                obj["m"] = string.Empty;
                obj["__updatePW"] = @"PandemicRIP";
                obj["d"] = string.Empty;
                obj["k"] = 1;
                obj["t"] = 7;
                obj["r"] = @"@ZA@d1";
                obj["v"] = @"S1";

                GameArray.Add(obj);
            }

            //// holdover from when the rewrite engine was used rather than directly setting this handler
            //if (context.Request.ServerVariables["HTTP_X_ORIGINAL_URL"] != null)
            //{
            //    responseObject["requestURL"] = context.Request.Url.GetLeftPart(UriPartial.Authority) + context.Request.ServerVariables["HTTP_X_ORIGINAL_URL"]; 
            //}
            //else
            //{
            //    responseObject["requestURL"] = context.Request.Url.ToString();
            //}

            responseObject["requestURL"] = context.Request.Url.ToString();

            context.Response.Write(responseObject.ToString(Newtonsoft.Json.Formatting.None));
        }
        
        private void PostGame(HttpContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream))
            {
                // read posted data
                JObject postedObject = JObject.Parse(reader.ReadToEnd());

                // process __gameId in input
                string inputGameId = postedObject["__gameId"].Value<string>();
                if (inputGameId == null || inputGameId.Length == 0)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    return;
                }

                // process __clientReqId in input
                if (postedObject["__clientReqId"] == null)
                {
                    context.Response.StatusCode = 200;
                    return;
                }
                int inputClientReqId = postedObject["__clientReqId"].Value<int>();

                // process input variables
                int    inputTimeoutSec  = postedObject["__timeoutSec"].Value<int>();
                string inputRowPW       = postedObject["__rowPW"].Value<string>();
                string inputAddr        = context.Request.UserHostAddress;

                // process input rowId if present
                long inputRowId = -1;
                if (postedObject["__rowId"] != null)
                {
                    inputRowId = postedObject["__rowId"].Value<int>();
                }

                // prepare variables for holding check data
                long   lookupRowId = -1;
                string lookupRowPw = string.Empty;

                if (inputRowId < 0)
                {
                    // no input row ID, so this game is either new or something's gone wrong, try to grab a rowId and rowPw
                    gamelist.CheckGame(inputAddr, inputClientReqId, out lookupRowId, out lookupRowPw);
                }
                else
                {
                    // grab the existing game's rowPw
                    gamelist.CheckGame(inputRowId, out lookupRowPw);
                    lookupRowId = inputRowId;
                }

                // no game already exists
                if (lookupRowId < 0)
                {
                    // process custom fields
                    Dictionary<string, JValue> customRowFields = new Dictionary<string, JValue>();
                    postedObject.Properties().ToList().ForEach(dr =>
                    {
                        if(!dr.Name.StartsWith("__"))
                        {
                            customRowFields[dr.Name] = (JValue)dr.Value;
                        }
                    });

                    // insert new game, get the rowId created
                    lookupRowId = gamelist.AddGame(inputGameId, DateTime.UtcNow, inputTimeoutSec, inputRowPW, inputClientReqId, inputAddr, customRowFields);
                }
                else if (inputRowPW == lookupRowPw)
                {
                    // process custom fields
                    Dictionary<string, JValue> customValues = new Dictionary<string, JValue>();
                    postedObject.Properties().ToList().ForEach(dr =>
                    {
                        if (!dr.Name.StartsWith("__"))
                        {
                            customValues[dr.Name] = (JValue)dr.Value;
                        }
                    });

                    // update game
                    gamelist.UpdateGame(lookupRowId, DateTime.UtcNow, inputTimeoutSec, inputClientReqId, inputAddr, customValues);
                }
                else
                {
                    context.Response.StatusCode = 400; // Bad Request
                    return;
                }

                // building response object
                JObject responseObject = new JObject();
                JObject responseSubObject = new JObject();
                responseObject["POST"] = responseSubObject;

                // send the data the caller needs to update this game later
                responseSubObject["__clientReqId"] = inputClientReqId;
                responseSubObject["__rowId"]       = lookupRowId;
                responseSubObject["__gameId"]      = inputGameId;

                // write response
                context.Response.Write(responseObject.ToString(Newtonsoft.Json.Formatting.None));
            }
        }

        private void DeleteGame(HttpContext context)
        {
            // check input rowId
            string rawRowId = context.Request.QueryString["__rowId"];
            if (rawRowId == null || rawRowId.Length == 0)
            {
                context.Response.StatusCode = 400; // Bad Request
                return;
            }

            // process input rowId
            long inputRowId = -1;
            if(!long.TryParse(rawRowId, out inputRowId))
            {
                context.Response.StatusCode = 400; // Bad Request
                return;
            }

            // process input rowPw
            string inputRowPw = context.Request.QueryString["__rowPW"];
            //if (inputRowPw == null || inputRowPw.Length == 0)
            //{
            //    context.Response.StatusCode = 400; // Bad Request
            //    return;
            //}
            if (inputRowPw == null)
            {
                inputRowPw = string.Empty;
            }

            // prepare variables for holding check data
            string lookupRowPw = string.Empty;

            // get RowPw for game
            gamelist.CheckGame(inputRowId, out lookupRowPw);
            if (lookupRowPw == null)
            {
                lookupRowPw = string.Empty;
            }

            if (inputRowPw == lookupRowPw)
            {
                // delete the game
                gamelist.DeleteGame(inputRowId);
            }
            else
            {
                context.Response.StatusCode = 400; // Bad Request
                return;
            }
        }

        // as the GameList class is just an adapater to a threadsafe
        // global instance we should be safe doing this
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}