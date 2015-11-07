using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasticWonderland.Domain
{
    public class SeashoreHelperFactory
    {
        private static SeashoreHelperFactory _instance;

        private SeashoreHelperFactory()
        {
        }

        public static SeashoreHelperFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SeashoreHelperFactory();
                }
                return _instance;
            }
        }

    }
}
