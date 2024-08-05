using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSync.Modules
{
    public class CardModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string iconName { get; set; }
        public ObservableCollection<DataType> Items { get; set; }
    }
}
