using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Merged
{
    [DataContract]
    internal class SenderObject
    {
        [DataMember]
        internal DateTime timeStamp;

        [DataMember]
        internal double procrastination;

        [DataMember]
        internal List<string> tags;

        [DataMember]
        internal string type;

        [DataMember]
        internal string userId;
    }
}
