using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace PlasticWonderland.Domain
{
    public class PhotoUploadWrapper
    {
        public PhotoUploadWrapper()
        {
        }

        public HttpStatusCode HttpResponseState { get; set; }
        public String UplUpdLink { get; set; }
    }
}
