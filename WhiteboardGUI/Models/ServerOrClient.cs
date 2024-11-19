using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteboardGUI.Services;
using WhiteboardGUI.ViewModel;

namespace WhiteboardGUI.Models
{
    public class ServerOrClient
    {
        public string userName;
        public int userId;
        private static readonly object padlock = new object();
        private static ServerOrClient _serverOrClient;
        public static ServerOrClient ServerOrClientInstance
        {
            get
            {
                lock (padlock)
                {
                    if (_serverOrClient == null)
                    {
                        _serverOrClient = new ServerOrClient();
                    }

                    return _serverOrClient;
                }
            }
        }



        public void SetUserDetails(string username, string userid)
        {
            userName = username;
            userId = int.Parse(userid);
        }
    }
}
