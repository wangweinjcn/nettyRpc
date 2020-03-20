using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NettyRPC.Core
{
    public class MessagePackDynamicSerializer : IDynamicJsonSerializer
    {
        public object Convert(object value, Type targetType)
        {
            throw new NotImplementedException();
        }

        public dynamic Deserialize(string json)
        {
            throw new NotImplementedException();
        }

        public string Serialize(object model)
        {
            throw new NotImplementedException();
        }
    }
}
