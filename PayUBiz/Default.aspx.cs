using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.IO;

namespace PayUBiz
{
    /*    
     * Note : It is recommended to fetch all the parameters from your Database rather than posting static values or entering them on the UI.
     * 
     * POST REQUEST to be posted to below mentioned PayU URLs:
     * 
     * For PayU Test Server:
     * POST URL: https://test.payu.in/_payment
    
     * For PayU Production (LIVE) Server:
    
     * POST URL: https://secure.payu.in/_payment
     * 
     * Set intial parameters in Web.config
    */
    public partial class _Default : System.Web.UI.Page
    {
        public string action1 = string.Empty;
        public string hash1 = string.Empty;
        public string txnid1 = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                //txnid.Value = DateTime.Now.ToString("HHmmssfffddMMyyyy");
                //txnid.Value = "807f1c4f-dd97-495e-5834-08da7291f1f6";
                key.Value = ConfigurationManager.AppSettings["MERCHANT_KEY"];

                if (!IsPostBack)
                {
                    frmError.Visible = false; // error form
                }
                else
                {
                    //frmError.Visible = true;
                }
                if (string.IsNullOrEmpty(Request.Form["hash"]))
                {
                    submit.Visible = true;
                }
                else
                {
                    submit.Visible = false;
                }
                // Required - Response callback url to be used by Payment Gateway to post back payment status. 
                // surl - for success tranction, furl - for failure transaction, curl - for cancel transaction.
                surl.Text = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + HttpRuntime.AppDomainAppVirtualPath + "ResponseHandling.aspx";
                furl.Text = surl.Text.ToString();
                curl.Text = furl.Text.ToString();
                // Contains information of integration type. Consult to PayU for more details.
                //udf5.Text = "PayUBiz_ASP.NET_Kit";
                //udf5.Text = "";
                //Not mandatory but fixed code can be passed to Payment Gateway to show default payment 
				//option tab. e.g. NB, CC, DC, CASH, EMI. Refer PDF for more details.
                //pg.Text = "CC";
            }
            catch (Exception ex)
            {
                Response.Write("<span style='color:red'>" + ex.Message + "</span>");

            }

        }

        /* Request Hash
	    ----------------
	    For hash calculation, you need to generate a string using certain parameters 
	    and apply the sha512 algorithm on this string. Please note that you have to 
	    use pipe (|) character as delimeter. 
	    The parameter order is mentioned below:
	
	    sha512(key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5||||||SALT)
	
	    Description of each parameter available on html page as well as in PDF.
	
	    Case 1: If all the udf parameters (udf1-udf5) are posted by the merchant. Then,
	    hash=sha512(key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5||||||SALT)
	
	    Case 2: If only some of the udf parameters are posted and others are not. For example, if udf2 and udf4 are posted and udf1, udf3, udf5 are not. Then,
	    hash=sha512(key|txnid|amount|productinfo|firstname|email||udf2||udf4|||||||SALT)

	    Case 3: If NONE of the udf parameters (udf1-udf5) are posted. Then,
	    hash=sha512(key|txnid|amount|productinfo|firstname|email|||||||||||SALT)
	
	    In present kit and available PayU plugins UDF5 is used. So the order is -	
	    hash=sha512(key|txnid|amount|productinfo|firstname|email|||||udf5||||||SALT)
	
	    */
        public string Generatehash512(string text)
        {
            byte[] message = Encoding.UTF8.GetBytes(text);
            //UnicodeEncoding UE = new UnicodeEncoding();
            byte[] hashValue;
            SHA512Managed hashString = new SHA512Managed();
            string hex = "";
            hashValue = hashString.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                hex += string.Format("{0:x2}", x);
            }
            return hex;
        }




        protected void Button1_Click(object sender, EventArgs e)
        {
            try
            {

                string[] hashVarsSeq;
                string hash_string = string.Empty;


                if (string.IsNullOrEmpty(Request.Form["txnid"])) // generating random txnid
                {
                    Random rnd = new Random();
                    string strHash = Generatehash512(rnd.ToString() + DateTime.Now);
                    txnid1 = strHash.ToString().Substring(0, 20);

                }
                else
                {
                    txnid1 = Request.Form["txnid"];
                }
                if (string.IsNullOrEmpty(Request.Form["hash"])) // generating hash value
                {
                    if (
                        string.IsNullOrEmpty(ConfigurationManager.AppSettings["MERCHANT_KEY"]) ||
                        string.IsNullOrEmpty(txnid1) ||
                        string.IsNullOrEmpty(Request.Form["amount"]) ||
                        string.IsNullOrEmpty(Request.Form["firstname"]) ||
                        string.IsNullOrEmpty(Request.Form["email"]) ||
                        string.IsNullOrEmpty(Request.Form["phone"]) ||
                        string.IsNullOrEmpty(Request.Form["productinfo"]) ||
                        string.IsNullOrEmpty(Request.Form["surl"]) ||
                        string.IsNullOrEmpty(Request.Form["furl"])
                        )
                    {
                        //error

                        frmError.Visible = true;
                        return;
                    }

                    else
                    {
                        frmError.Visible = false;
                        hashVarsSeq = ConfigurationManager.AppSettings["hashSequence"].Split('|'); // spliting hash sequence from config
                        hash_string = "";
                        foreach (string hash_var in hashVarsSeq)
                        {
                            if (hash_var == "key")
                            {
                                hash_string = hash_string + ConfigurationManager.AppSettings["MERCHANT_KEY"];
                                hash_string = hash_string + '|';
                            }
                            else if (hash_var == "txnid")
                            {
                                hash_string = hash_string + txnid1;
                                hash_string = hash_string + '|';
                            }
                            else if (hash_var == "amount")
                            {
                                hash_string = hash_string + Convert.ToDecimal(Request.Form[hash_var]).ToString("g29");
                                hash_string = hash_string + '|';
                            }
                            else
                            {

                                hash_string = hash_string + (Request.Form[hash_var] != null ? Request.Form[hash_var].Trim() : string.Empty);// isset if else
                                hash_string = hash_string + '|';
                            }
                        }

                        hash_string += ConfigurationManager.AppSettings["SALT"];// appending SALT

                        hash1 = Generatehash512(hash_string).ToLower();         //generating hash
                        action1 = ConfigurationManager.AppSettings["PAYU_BASE_URL"] + "/_payment";// setting URL

                    }


                }

                else if (!string.IsNullOrEmpty(Request.Form["hash"]))
                {
                    hash1 = Request.Form["hash"];
                    action1 = ConfigurationManager.AppSettings["PAYU_BASE_URL"] + "/_payment";

                }

                if (!string.IsNullOrEmpty(hash1))
                {
                    hash.Value = hash1;
                    txnid.Value = txnid1;

                    System.Collections.Hashtable data = new System.Collections.Hashtable(); // adding values in gash table for data post
                    data.Add("hash", hash.Value);
                    data.Add("key", key.Value);
                    data.Add("txnid", txnid.Value);
                    string AmountForm = Convert.ToDecimal(amount.Text.Trim()).ToString("g29");// eliminating trailing zeros
                    amount.Text = AmountForm;
                    data.Add("amount", AmountForm);
                    data.Add("firstname", firstname.Text.Trim());
                    data.Add("email", email.Text.Trim());
                    data.Add("phone", phone.Text.Trim());
                    data.Add("productinfo", productinfo.Text.Trim());
                    data.Add("surl", surl.Text.Trim());
                    data.Add("furl", furl.Text.Trim());
                    //data.Add("lastname", lastname.Text.Trim());
                    data.Add("curl", curl.Text.Trim());
                    //data.Add("address1", address1.Text.Trim());
                    //data.Add("address2", address2.Text.Trim());
                    //data.Add("city", city.Text.Trim());
                    //data.Add("state", state.Text.Trim());
                    //data.Add("country", country.Text.Trim());
                    //data.Add("zipcode", zipcode.Text.Trim());
                    data.Add("udf1", udf1.Text.Trim());
                    data.Add("udf2", udf2.Text.Trim());
                    data.Add("udf3", udf3.Text.Trim());
                    data.Add("udf4", udf4.Text.Trim());
                    data.Add("udf5", udf5.Text.Trim());
                    data.Add("pg", pg.Text.Trim());


                    string strForm = PreparePOSTForm(action1, data);
                    Page.Controls.Add(new LiteralControl(strForm));

                }

                else
                {
                    //no hash

                }

            }

            catch (Exception ex)
            {
                Response.Write("<span style='color:red'>" + ex.Message + "</span>");

            }



        }
        private string PreparePOSTForm(string url, System.Collections.Hashtable data)      // post form
        {
            //Set a name for the form
            string formID = "PostForm";
            //Build the form using the specified data to be posted.
            StringBuilder strForm = new StringBuilder();
            strForm.Append("<form id=\"" + formID + "\" name=\"" +
                           formID + "\" action=\"" + url +
                           "\" method=\"POST\">");

            foreach (System.Collections.DictionaryEntry key in data)
            {

                strForm.Append("<input type=\"hidden\" name=\"" + key.Key +
                               "\" value=\"" + key.Value + "\">");
            }


            strForm.Append("</form>");
            //Build the JavaScript which will do the Posting operation.
            StringBuilder strScript = new StringBuilder();
            strScript.Append("<script language='javascript'>");
            strScript.Append("var v" + formID + " = document." +
                             formID + ";");
            strScript.Append("v" + formID + ".submit();");
            strScript.Append("</script>");
            //Return the form and the script concatenated.
            //(The order is important, Form then JavaScript)
            return strForm.ToString() + strScript.ToString();
        }

    }
}
