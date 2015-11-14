using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaShoreShared
{
    public class SharedDbFactory
    {
        private static SharedDbFactory _instance;

        private SharedDbFactory()
        {
        }

        public static SharedDbFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SharedDbFactory();
                }
                return _instance;
            }
        }


    }
}
