using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeRedis.Model
{
    //1) 1) "Palermo"
    //   2) "190.4424"
    //   3) (integer) 3479099956230698
    //   4) 1) "13.361389338970184"
    //      2) "38.115556395496299"
    //2) 1) "Catania"
    //   2) "56.4413"
    //   3) (integer) 3479447370796909
    //   4) 1) "15.087267458438873"
    //      2) "37.50266842333162"
    public class GeoRadiusResult
    {
        public string member;
        public decimal? dist;
        public decimal? hash;
        public decimal? longitude;
        public decimal? latitude;
    }
}
