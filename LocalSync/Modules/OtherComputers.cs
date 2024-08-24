namespace LocalSync.Modules
{
    public class ComputerDataType : Modules.DataType
    {
        public string Name { get; private set; }
        public string dataType { get; private set; }
        public string dataFileIcon { get; private set; }

        public ComputerDataType(string name, string dataType, string dataFileIcon)
        {
            Name = name;
            this.dataType = dataType;
            this.dataFileIcon = dataFileIcon;
        }
    }
}
