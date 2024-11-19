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
   public class SnapShotDownloadItem
    {
        public SnapShotDownloadItem(string snapShotFileName, DateTime dateTime)
        {
            FileName = snapShotFileName;
            Time = dateTime;
        }

        public string FileName { get; set; }
        public DateTime Time { get; set; }
    }
    public class SnapShot
    {
        public string userID;
        public string fileName;
        public DateTime dateTime;

        [JsonConverter(typeof(ShapeConverter))]
        public ObservableCollection<IShape> Shapes;

        public SnapShot() { }
    }
}
