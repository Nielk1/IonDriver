using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raknet
{
    public interface IGameListPlugin
    {
        string GameID { get; }
        string Name { get; }
        Version Version { get; }
        string DisplayName { get; }

        /// <summary>
        /// Change to alter QueryString values.
        /// </summary>
        /// <param name="queryString"></param>
        void InterceptQueryStringForGet(ref NameValueCollection queryString);

        /// <summary>
        /// Alterations to the game list may be made here immediatly after the database lookup
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="rawGames"></param>
        void PreProcessGameList(NameValueCollection queryString, ref List<GameData> rawGames);



        //double GetLastResult { get; }
        //double Execute(double value1, double value2);

        //event EventHandler OnExecute;

        //void ExceptionTest(string input);
    }
}
