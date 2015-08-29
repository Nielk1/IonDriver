using System;
using System.Collections.Generic;
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

        List<GameData> PreProcessGameList(List<GameData> rawGames);



        //double GetLastResult { get; }
        //double Execute(double value1, double value2);

        //event EventHandler OnExecute;

        //void ExceptionTest(string input);
    }
}
