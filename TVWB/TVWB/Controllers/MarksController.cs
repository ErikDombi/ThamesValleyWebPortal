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
    public class MarksController : Controller
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
            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET&__EVENTARGUMENT&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={user}&txtPwd={pass}&btnSubmit=Login");

            var z = await GetLocation(uri.ToString());
            var x = await GetCookies(z);
            Response.Cookies.Append("Token", x[0].Value);

            WebRequest webRequest = WebRequest.Create("https://schoolapps2.tvdsb.ca/students/portal_secondary/student_Info/stnt_transcript.asp");
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
            var tbl = htmlDoc.DocumentNode.Descendants("table").ElementAt(1).Descendants("tr").Skip(3);
            List<Marks> marks = new List<Marks>();
            foreach(var mark in tbl.Select(t => t.Descendants("td")))
            {
                if (mark.ToList().Count == 9) {
                    if (mark.ElementAt(0).InnerText == "Date")
                        continue;
                    Marks m = new Marks()
                    {
                        Date = mark.ElementAt(0).InnerText.Replace("&nbsp;", ""),
                        Course = mark.ElementAt(1).InnerText.Replace("&nbsp;", ""),
                        Mark = mark.ElementAt(2).InnerText.Replace("&nbsp;", ""),
                        Comment = mark.ElementAt(3).InnerText.Replace("&nbsp;", ""),
                        Ind = mark.ElementAt(4).InnerText.Replace("&nbsp;", ""),
                        Team = mark.ElementAt(5).InnerText.Replace("&nbsp;", ""),
                        Org = mark.ElementAt(6).InnerText.Replace("&nbsp;", ""),
                        Homework = mark.ElementAt(7).InnerText.Replace("&nbsp;", ""),
                        Init = mark.ElementAt(8).InnerText.Replace("&nbsp;", ""),

                    };
                    marks.Add(m);
                }else {
                    Marks m = new Marks()
                    {
                        Divider = mark.ElementAt(0).InnerText.Substring(0, 4) + "-" + mark.ElementAt(0).InnerText.Substring(4)
                    };
                    marks.Add(m);
                }
                
            }
            return View(model: marks);
        }
    }
}
