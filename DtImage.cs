using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Enfiyat
{
    public class DtImage
    {
        public Bitmap Convert(string ImageUrl) 
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(ImageUrl);
            myRequest.Method = "GET";
            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
            Bitmap bmp = new Bitmap(myResponse.GetResponseStream());
            myResponse.Close();
            return bmp;
        }
    }
}
