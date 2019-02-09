using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TVWB.Models;
using System.Net.Http;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Text;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TVWB.Controllers
{
    public class AttendanceController : Controller
    {
        public bool CorrectLogin(string username, string password)
        {
            WebRequest webRequest = WebRequest.Create($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET&__EVENTARGUMENT&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={username}&txtPwd={password}&btnSubmit=Login");
            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

            var encoding = ASCIIEncoding.ASCII;
            string responseText;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                responseText = reader.ReadToEnd();
            }
            return !responseText.Contains("Login Failed");
        }

        public async Task<string> GetLocation(string url)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            using (HttpClient client = new HttpClient(httpClientHandler))
            {
                var response = await client.GetAsync(url);
                return response.Headers.GetValues("Location").FirstOrDefault();
            }
        }

        private async Task<List<Cookie>> GetCookies(string url)
        {
            var cookieContainer = new CookieContainer();
            var uri = new Uri(url);
            using (var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            })
            {
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    await httpClient.GetAsync(uri);
                    return cookieContainer.GetCookies(uri).Cast<Cookie>().ToList();
                }
            }
        }
        // GET: /<controller>/
        public async Task<IActionResult> Index()
        {
            string user = Request.Cookies["Username"];
            string pass = Request.Cookies["Password"];
            if (string.IsNullOrEmpty(user))
                return Redirect("/Login/");
            Console.WriteLine("------------------------------------");
            Console.WriteLine($"{user} | {pass}");
            Console.WriteLine("------------------------------------");
            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET&__EVENTARGUMENT&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={user}&txtPwd={pass}&btnSubmit=Login");

            var z = await GetLocation(uri.ToString());
            var x = await GetCookies(z);
            Response.Cookies.Append("Token", x[0].Value);

            WebRequest webRequest = WebRequest.Create("https://schoolapps2.tvdsb.ca/students/portal_secondary/student_Info/stnt_attendance.asp");
            foreach (var cookie in x)
            {
                webRequest.TryAddCookie(new Cookie(cookie.Name, cookie.Value, "/", "schoolapps2.tvdsb.ca"));
            }
            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            var encoding = ASCIIEncoding.ASCII;
            string responseText;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                responseText = reader.ReadToEnd();
            }
            htmlDoc.LoadHtml(responseText);
            var classesMissed = htmlDoc.DocumentNode.Descendants("table").ElementAt(1).Descendants("tr").Skip(1).Take(htmlDoc.DocumentNode.Descendants("table").ElementAt(1).Descendants("tr").ToList().Count - 2);
            List<Absence> classesMissedList = new List<Absence>();
            for(int i = 0; i < classesMissed.ToList().Count; i++)
            {
                Console.WriteLine(classesMissed.ElementAt(i).InnerText);
                classesMissedList.Add(new Absence()
                {
                    Date = classesMissed.ElementAt(i).ChildNodes[0].InnerText,
                    Period = classesMissed.ElementAt(i).ChildNodes[1].InnerText.Substring(0, 1),
                    Class = classesMissed.ElementAt(i).ChildNodes[2].InnerText,
                    Code = classesMissed.ElementAt(i).ChildNodes[3].InnerText,
                    Reason = classesMissed.ElementAt(i).ChildNodes[4].InnerText
                });
            }
            AbsentsInfo ai = new AbsentsInfo();
            ai.Absences = classesMissedList;
            ai.Absents = htmlDoc.DocumentNode.Descendants("table").ElementAt(1).Descendants("tr").LastOrDefault().Descendants().FirstOrDefault(t => t.InnerText.Contains("Absents = ")).InnerText.Split(" ").LastOrDefault();
            ai.Lates = htmlDoc.DocumentNode.Descendants("table").ElementAt(1).Descendants("tr").LastOrDefault().Descendants().FirstOrDefault(t => t.InnerText.Contains("Lates = ")).InnerText.Split(" ").LastOrDefault();
            return View(model:ai);
        }
    }
}
