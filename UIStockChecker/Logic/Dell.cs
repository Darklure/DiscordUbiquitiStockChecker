using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UIStockChecker.Logic
{
    // just a test class with HtmlAgilityPack

    internal class Dell
    {

        private static string url = "https://www.dell.com/en-us/search/AW3423DW?r=43280&p=1&ac=facetselect&t=Product&c=4009&f=true";

        public static void GetMonitor()
        {
            var web = new HtmlWeb();
            var doc = web.Load(url);
            //var nodes = doc.DocumentNode.SelectNodes("//table[@class='tblContent']//td");
            
            var nodes = doc.DocumentNode.SelectNodes("//[@id='ps - wrapper']");
        }

    }
}
