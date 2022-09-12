using System;
using System.Data;
using System.Web.Script.Serialization;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.IO;

/*Note : After completing transaction process it is recommended to make an enquiry call with PayU to 
 * validate the response received and then save the response to DB or display it on UI*/

public partial class ResponseHandling : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        /* Response received from Payment Gateway at this page.
        It is absolutely mandatory that the hash (or checksum) is computed again after you receive response from PayU and compare it with request and post back parameters. This will protect you from any tampering by the user and help in ensuring a safe and secure transaction experience. It is mandate that you secure your integration with PayU by implementing Verify webservice and Webhook/callback as a secondary confirmation of transaction response.
		
		Process response parameters to generate Hash signature and compare with Hash sent by payment gateway 
        to verify response content. Response may contain additional charges parameter so depending on that 
        two order of strings are used in this kit.
    
        Hash string without Additional Charges -
        hash = sha512(SALT|status||||||udf5|||||email|firstname|productinfo|amount|txnid|key)

        With additional charges - 
        hash = sha512(additionalCharges|SALT|status||||||udf5|||||email|firstname|productinfo|amount|txnid|key)

        */
        try
        {
        
        string [] merc_hash_vars_seq ;
        string merc_hash_string = string.Empty;
        string merc_hash = string.Empty;
        string order_id = string.Empty;         
        string hash_seq="key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5|udf6|udf7|udf8|udf9|udf10";

        if (Request.Form["status"]=="success")
        {
        
            merc_hash_vars_seq = hash_seq.Split('|');
            Array.Reverse(merc_hash_vars_seq);
            merc_hash_string = ConfigurationManager.AppSettings["SALT"] + "|" + Request.Form["status"];
            //Check for presence of additionalCharges and include in hash
            if (Request.Form["additionalCharges"] != null)
                merc_hash_string = Request.Form["additionalCharges"] + "|" +ConfigurationManager.AppSettings["SALT"] + "|" + Request.Form["status"];

            foreach (string merc_hash_var in merc_hash_vars_seq)
            {
                merc_hash_string += "|";
                merc_hash_string = merc_hash_string +  (Request.Form[merc_hash_var]!=null ? Request.Form[merc_hash_var] : "");

            }
            //Calculate response hash to verify	
            merc_hash = Generatehash512(merc_hash_string).ToLower();


            //Comapre status and hash. Hash verification is mandatory.
            if (merc_hash!=Request.Form["hash"])
            {
                Response.Write("<h2>Hash value did not match</h2>");
                
            }
            else
            {
                order_id = Request.Form["txnid"];
                Response.Write("<h2>Payment Response-</h2><br />");

                foreach (string strKey in Request.Form)
                {
                    Response.Write(strKey);
                    Response.Write( "=");
                    Response.Write(Request.Form[strKey].ToString());
                    Response.Write("<br />");
                }
                Response.Write("<h2>Hash Verified...</h2><br />");

                if(VerifyPayment(order_id,Request.Form["mihpayid"].ToString()))
                    Response.Write("<h2>Payment Verified...</h2><br />");
                else
                    Response.Write("<h2>Payment Verification Failed...</h2><br />");
                //Hash value did not matched
            }

        }

        else
            {

                Response.Write("<h2>Payment failed or cancelled</h2>");
           // osc_redirect(osc_href_link(FILENAME_CHECKOUT, 'payment' , 'SSL', null, null,true));
            
            }
        }

        catch( Exception ex)
        {
            Response.Write("<span style='color:red'>" + ex.Message + "</span>");

        }
    }

    //This function is used to double check payment
    public Boolean VerifyPayment(string txnid, string mihpayid)
    {
        string command = "verify_payment";
        string hashstr = ConfigurationManager.AppSettings["MERCHANT_KEY"] + "|" + command + "|" + txnid + "|" + ConfigurationManager.AppSettings["SALT"];

        string hash = Generatehash512(hashstr);

        ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol = (SecurityProtocolType)768 | (SecurityProtocolType)3072;
        
        var request = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["PAYU_VERIFY_URL"]);

        var postData = "key=" + Uri.EscapeDataString(ConfigurationManager.AppSettings["MERCHANT_KEY"]);
        postData += "&hash=" + Uri.EscapeDataString(hash);
        postData += "&var1=" + Uri.EscapeDataString(txnid);
        postData += "&command=" + Uri.EscapeDataString(command);
        var data = Encoding.ASCII.GetBytes(postData);

        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = data.Length;

        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        var response = (HttpWebResponse)request.GetResponse();

        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

        if (responseString.Contains("\"mihpayid\":\"" + mihpayid + "\"") && responseString.Contains("\"status\":\"success\""))
            return true;
        else
            return false;
        /*
		Here is json response example -
		
		{"status":1,
		"msg":"1 out of 1 Transactions Fetched Successfully",
		"transaction_details":</strong>
		{	
			"Txn72738624":
			{
				"mihpayid":"403993715519726325",
				"request_id":"",
				"bank_ref_num":"670272",
				"amt":"6.17",
				"transaction_amount":"6.00",
				"txnid":"Txn72738624",
				"additional_charges":"0.17",
				"productinfo":"P01 P02",
				"firstname":"Viatechs",
				"bankcode":"CC",
				"udf1":null,
				"udf3":null,
				"udf4":null,
				"udf5":"PayUBiz_PHP7_Kit",
				"field2":"179782",
				"field9":" Verification of Secure Hash Failed: E700 -- Approved -- Transaction Successful -- Unable to be determined--E000",
				"error_code":"E000",
				"addedon":"2019-08-09 14:07:25",
				"payment_source":"payu",
				"card_type":"MAST",
				"error_Message":"NO ERROR",
				"net_amount_debit":6.17,
				"disc":"0.00",
				"mode":"CC",
				"PG_TYPE":"AXISPG",
				"card_no":"512345XXXXXX2346",
				"name_on_card":"Test Owenr",
				"udf2":null,
				"status":"success",
				"unmappedstatus":"captured",
				"Merchant_UTR":null,
				"Settled_At":"0000-00-00 00:00:00"
			}
		}
		}
		
		Decode the Json response and retrieve "transaction_details" 
		Then retrieve {txnid} part. This is dynamic as per txnid sent in var1.
		Then check for mihpayid and status.
		
		*/
    }

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
                hex += String.Format("{0:x2}", x);
            }
            return hex;

    }

}