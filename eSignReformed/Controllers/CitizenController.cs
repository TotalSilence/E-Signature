﻿using eSignReformed.Models;
using IronPdf;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace eSignReformed.Controllers
{
    public class CitizenController : Controller
    {
        private eReformedEntities db = new eReformedEntities();

        public static citizen c = new citizen();
        private SqlConnection sqlconn = new SqlConnection(ConfigurationManager.ConnectionStrings["eReformed"].ConnectionString);

        public ActionResult portal()
        {
            return View();
        }

        //This sends the mail and execute the JS to enable the div2 and disable div1 and div2

        [HttpPost]
        public void sendotp(FormCollection col)
        {
            // sqlconn.Open();
            long adno = Convert.ToInt64(col["adno"].ToString());
            string token = "";
            citizen cz = db.citizens.Find(adno);
            if (cz != null)
            {
                Session["ano"] = adno;
                Session.Timeout = 30;
                if (Session.Mode.ToString() == "off")
                {
                    Redirect("~/Views/Citizen/portal");
                }
                c = cz;
                token = generateotp();
                Session["otp"] = token.ToString();  //Session stores the otp for
                Sendmail(cz, token);
            }
            else
            {
                RedirectToAction("anotfound");
            }
        }

        public ActionResult anotfound()
        {
            return View();
        }

        public string generateotp()    //Generates OTP Cryptographically
        {
            StringBuilder token = new StringBuilder();
            using (RNGCryptoServiceProvider rngcsp = new RNGCryptoServiceProvider())
            //Generating a random Number
            {
                byte[] data = new byte[4];
                for (int i = 0; i < 6; i++)
                {
                    rngcsp.GetBytes(data);
                    char randomchar = Convert.ToChar(((BitConverter.ToUInt32(data, 0)) % 20) + 65);
                    token.Append(randomchar);
                }
            }
            return token.ToString();
        }

        public string tokens(string token)
        {
            token = Session["otp"].ToString();

            return token;
        }

        [HttpPost]
        public ActionResult UploadControl(HttpPostedFileBase file)
        {
            if (file.ContentLength > 0 && file != null && file.ContentLength < 214748364)
            {
                long a = Convert.ToInt64(Session["ano"].ToString());
                string extension = Path.GetExtension(file.FileName);
                string fileName = Session["ano"].ToString() + c.nooffiles + extension;
                //updating no. of files in data base
                sqlconn.Open();
                c.nooffiles = c.nooffiles + 1;
                SqlCommand cmd = new SqlCommand("update citizens set nooffiles=@n where adno = @a", sqlconn);
                cmd.Parameters.AddWithValue("@n", c.nooffiles);
                cmd.Parameters.AddWithValue("@a", a);
                cmd.ExecuteNonQuery();

                file.SaveAs(Path.Combine(Server.MapPath(@"~/PDF/"), fileName));
                signeddoc();
                return View("portal");
            }
            else
            {
                Session.RemoveAll();
                Session.Abandon();
                return Redirect(Request.UrlReferrer.ToString());
                //Invalid File
            }
        }

        [HttpPost]
        public void verotp(string otp)
        {
            if (!verify(otp))
            {
                // OTp is wrong
            }
            else
            {
                //OTP is right
            }
        }

        //Signing the document
        public void signeddoc()
        {
            long adno = Convert.ToInt64(Session["ano"].ToString());
            //document signing

            //File 1
            string file1 = Session["ano"].ToString() + (c.nooffiles - 1) + ".pdf";
            string path1 = Path.Combine(Server.MapPath(@"~/PDF/"), file1);

            string signedfilename = "Signed" + Session["ano"].ToString() + (c.nooffiles - 1) + ".pdf";

            PdfDocument Pdf = new PdfDocument(path1);
            string signpath = Server.MapPath("~/verifiedsmalll.png");

            int pc = Pdf.PageCount;
            string html = "<img src ='" + signpath + "'  />";
            HtmlStamp s = new HtmlStamp() { Html = html, Opacity = 60, AutoCenterStampContentOnStampCanvas = true, ZIndex = HtmlStamp.StampLayer.BehindExistingPDFContent, Height = 50, Width = 50 };
            Pdf.StampHTML(s);
            string savedpth = Path.Combine(Server.MapPath(@"~/SignedPDFs/"), signedfilename);
            Pdf.SaveAs("~/SignedPDFs/" + signedfilename);

            //Try to print the document
            PdfDocument Pdf1 = new PdfDocument(savedpth);
            Pdf1.Print();
            Session.RemoveAll();
            Session.Abandon();
            RedirectToAction("portal");
            sqlconn.Close();
        }

        //Function to verify the otp

        public bool verify(string otp)
        {
            if (otp.ToString() == Session["otp"].ToString())
            {
                return true;
            }
            else
            { return false; }
        }

        //Send e-mail to the registered email address
        public void Sendmail(citizen p, string token)
        {
            SmtpClient smtpClient = new SmtpClient("smtp.live.com", 587);

            smtpClient.UseDefaultCredentials = true;
            smtpClient.Credentials = new System.Net.NetworkCredential("sterntest0993@outlook.com", "Stern1988", "outlook.com");
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = true;
            MailMessage mail = new MailMessage();
            mail.Body = "The Code for Authentication is " + token.ToString() + ". Use it to verify yourself.";
            //Setting From and To
            mail.Subject = "e-Sign Authorization";
            mail.From = new MailAddress("sterntest0993@outlook.com");
            // mail.To.Clear();
            mail.To.Add(new MailAddress(p.email.ToString()));
            smtpClient.Send(mail);
            Response.Write("<script>alert('hi')</script>");
        }
    }
}