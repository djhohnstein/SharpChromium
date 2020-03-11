using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace SharpChrome
{
    public class HistoricUrl
    {
        string Url;
        string Title;
        int VisitCount;
        HostCookies Cookies;

        public HistoricUrl(DataRow row, HostCookies[] cookies)
        {
            Url = row["url"].ToString();
            Title = row["title"].ToString();
            int.TryParse(row["visit_count"].ToString(), out VisitCount);
            Cookies = HostCookies.FilterHostCookies(cookies, Url);
        }

        public void Print()
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");
            Console.WriteLine("--- Chrome History (User: {0}) ---", user);
            int spaces = 15;
            Console.WriteLine("{0}{1}: {2}", "URL", spaces - 3, Url);
            Console.WriteLine("{0}{1}: {2}", "Title", spaces - 5, Title == "" ? "No Title" : Title);
            Console.WriteLine("{0}{1}: {2}", "Visit Count", spaces - 11, VisitCount);
            Console.WriteLine("{0}{1}: {2}", "Cookies", spaces - 7, Cookies.ToJSON());
            Console.WriteLine();
        }

    }
}
