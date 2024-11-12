using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteboardGUI.Services;

namespace WhiteboardGUI.Models
{
    public class SnapShot
    {
        public string userID;
        public string fileName;

        [JsonConverter(typeof(ShapeConverter))]
        public ObservableCollection<IShape> Shapes;

        public SnapShot() { }
        public SnapShot(double clientID, ObservableCollection<IShape> shapes, String filename)
        {
            this.fileName = fileName;
            this.userID = clientID.ToString();
            Shapes = shapes;
        }
    }
}
