using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace raknet
{
    public class Global : System.Web.HttpApplication
    {
        public SqliteInterface SingleThreadDatabaseInterface = SqliteInterface.Instance;
        public Dictionary<string, IGameListPlugin> GameListPlugins = new Dictionary<string, IGameListPlugin>();

        public Global() : base()
        {
            List<string> plugins = Directory.GetFiles(ConfigurationManager.AppSettings[@"GameListPluginPath"], "*.dll", SearchOption.TopDirectoryOnly).ToList();
            plugins.ForEach(dr =>
            {
                try
                {
                    IGameListPlugin plugin = LoadAssembly(dr);
                    GameListPlugins.Add(plugin.GameID, plugin);
                }
                catch { }
            });
        }

        private IGameListPlugin LoadAssembly(string assemblyPath)
        {
            string assembly = Path.GetFullPath(assemblyPath);
            Assembly ptrAssembly = Assembly.LoadFile(assembly);
            foreach (Type item in ptrAssembly.GetTypes())
            {
                if (!item.IsClass) continue;
                if (item.GetInterfaces().Contains(typeof(IGameListPlugin)))
                {
                    return (IGameListPlugin)Activator.CreateInstance(item);
                }
            }
            throw new Exception("Invalid DLL, Interface not found!");
        }

        protected void Application_Start(object sender, EventArgs e)
        {

        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            
        }
    }
}