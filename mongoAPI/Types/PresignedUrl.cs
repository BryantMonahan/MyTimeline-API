using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mongoAPI.Types
{
    public class PresignedUrl
    {
        public string Key { get; set; }
        public string Url { get; set; }
    }
}