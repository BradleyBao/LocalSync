using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSync.Modules
{
    public class OtherComputersGrid : INotifyPropertyChanged
    {
        public string deviceName {  get; set; }
        public string deviceIP { get; set; }
        public string iconName { get; set; }
        public DateTime LastHeartbeat { get; set; } = DateTime.Now;

        public OtherComputersGrid(string deviceName, string deviceIP) {
            this.deviceName = deviceName;
            this.deviceIP = deviceIP;
            this.iconName = "\uE7F8"; 
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
