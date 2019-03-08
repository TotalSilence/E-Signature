﻿using eSignReformed.Models;
using IronPdf;
using System;
using System.Collections.Generic;
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

        /*
        // GET: Citizen
        public ActionResult Index()
        {
            return View();
        }

        // GET: Citizen/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Citizen/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Citizen/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Citizen/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Citizen/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Citizen/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Citizen/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }*/

        public ActionResult portal()
        {
            Session.RemoveAll();
            Session.Abandon();
            return View();
        }

        //This sends the mail and execute the JS to enable the div2 and disable div1 and div2

        [HttpPost]
        public void sendotp(FormCollection col)
        {
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

                token = generateotp();
                Session["otp"] = token.ToString();  //Session stores the otp for
                Sendmail(cz, token);
            }
            else
            {
                Response.Write("<script>alert('This aadhar number is not found.\nEnsure the aadhar number entered is correct.\nEnsure to enroll for aadhar number.')</script>");
            }
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

        [HttpPost]
        public void UploadControl(HttpPostedFileBase file)
        {
            
            if (file.ContentLength > 0 && file != null)
            {
                string extension = Path.GetExtension(file.FileName);
                string fileName = Session["ano"].ToString() + extension;
                file.SaveAs(Path.Combine(Server.MapPath(@"~/PDF/"), fileName));
                signeddoc();
                
            }
            else
            {
                Session.RemoveAll();
                Session.Abandon();
                Response.Redirect("https://www.google.com");//Test to observe the result of the above condition
            }
            
            
        }

        [HttpPost]
        public void verotp(FormCollection col)
        {
            string otp = col["otp"].ToString();
            if (!verify(otp))
            {
                Session.RemoveAll();
                Session.Abandon();
                //Redirect to Portal
                
                
            }
            else
            {
                Response.Redirect("https://www.duckduckgo.com");
            }
        }

        //Signing the document
        public void signeddoc()
        {
            long adno = Convert.ToInt64(Session["ano"].ToString());
            //document signing
            //List<PdfDocument> pdfs = new List<PdfDocument>();

            //File 1
            string file1 = Session["ano"].ToString() + ".pdf";
            string path1 = Path.Combine(Server.MapPath(@"~/PDF/"), file1);
            //pdfs.Add(PdfDocument.FromFile(path1));

            string filename = "Signed" + Session["ano"].ToString() + ".pdf";

            string sname = "signature" + Session["ano"].ToString() + ".png";

            string signpath = Path.Combine(Server.MapPath("~/Signatures/"), sname);

            HtmlToPdf Renderer1 = new HtmlToPdf();

            PdfDocument Pdf = new PdfDocument(path1);

            int pc = Pdf.PageCount;

            HtmlStamp SignatureStamp = new HtmlStamp() { Html = "<img src='" + signpath + "' />", Width = 50, Height = 50, Bottom = 10, Right = 10, ZIndex = HtmlStamp.StampLayer.OnTopOfExistingPDFContent };
            Pdf.StampHTML(SignatureStamp, pc - 1);
            Pdf.SaveAs("~/SignedPDFs/" + filename);

            //Try to print the document
            
            HtmlToPdf Renderer = new HtmlToPdf();
            string pth = @"C:\Users\chait\source\repos\eSign3\eSign3\SignedPDFs\" + filename;
            PdfDocument Pdf1 = new PdfDocument(pth);
            Pdf1.Print();
            Session.RemoveAll();
            Session.Abandon();
            RedirectToAction("portal");
        }

        //Function to verify the otp

        public bool verify(string otp)
        {
            if (otp.ToString() == "OTPOTP" )  //Session["otp"].ToString()
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
        }
    }
}