using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSync.Modules
{
    public interface DataType
    {
        public string Name { get; }
        public string dataType { get; }
        public string dataFileIcon { get; }
    }
}
