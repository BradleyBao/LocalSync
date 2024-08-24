using System.Collections.ObjectModel;

namespace LocalSync.Modules
{
    public class CardModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string iconName { get; set; }
        public string navPage {  get; set; }
        public ObservableCollection<DataType> Items { get; set; }
    }
}
