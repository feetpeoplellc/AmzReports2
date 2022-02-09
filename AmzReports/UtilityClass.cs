using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.XPath;
using System.Web;
using System.Net;
using System.Xml.Linq;


// using System.Runtime.InteropServices;

namespace AmzReports
{
    public static class UtilityClass
    {
        public static string StringToHex(string s)
        {
            byte[] ba = Encoding.Default.GetBytes(s);
            string hexString = BitConverter.ToString(ba);
            return hexString.Replace("-", "");
        }

        public static Dictionary<string,string> GetArgumentValues(string[] arguments)
        {
            Dictionary<string, string> argParameters = new Dictionary<string, string>();

            foreach (string s in arguments)
            {
                // isSpecialArgument = false;
                // Show the Help File
                if (s.ToUpper().StartsWith("/H") == true)
                {
                    Console.WriteLine("amzReports v1.0");
                    Console.WriteLine("Utility that will launch a report request on Seller Central and retrieve when finished.");
                    Console.WriteLine("");
                    Console.WriteLine("Usage:");
                    Console.WriteLine("amzReports (Option)");
                    Console.WriteLine("");
                    Console.WriteLine("Options");
                    Console.WriteLine("/H   Shows the help screen and exits program");
                    Console.WriteLine("/StartDate=\"<StartDate>\"");
                    Console.WriteLine("/EndDate=\"<End Date>\"");
                    Console.WriteLine("/ReportOptions=\"<Report Options>\"");
                    Console.WriteLine("/CountryId=\"<Your Country Abbreviation>\"");
                    Console.WriteLine("");
                    Console.WriteLine("Required Options");
                    Console.WriteLine("/ReportType=\"<ReportType>\"");
                    Console.WriteLine("/AwsAccessKeyId=\"<Amazon Access Key ID>\"");
                    Console.WriteLine("/AwsSecretKey=\"<Secret Key provided by Amazon>\"");
                    Console.WriteLine("/MwsAuthToken=\"<MWS Auth Token\"");
                    Console.WriteLine("/SellerId=\"<Amazon Seller ID\"");
                    Console.WriteLine("/FileName=\"<Full Path and Filename where report will be saved>\"");
                    // isSpecialArgument = true;
                    // Exit program
                    System.Environment.Exit(1);
                }

                // CountryID
                if (s.ToUpper().StartsWith("/COUNTRYID") == true)
                {
                    (string marketplaceId, string marketplaceUrl) = UtilityClass.getMarketplaceInfo(s.ToUpper().Substring(11, s.Length - 11));
                    argParameters.Add("MARKETPLACEID", marketplaceId);
                    argParameters.Add("MARKETPLACEURL", marketplaceUrl);
                }

                // ***************************************************************
                // Get the Parameter Names and Values and Add to the Dictionary
                // ***************************************************************

                (string paramName, string paramValue) = UtilityClass.getArgumentValue(s);
                // Console.WriteLine("ParamName: {0}, Value: {1}", paramName, paramValue);
                if (string.IsNullOrEmpty(paramValue))
                {
                    // Then do not add it to the parameters list
                }
                else
                {
                    argParameters.Add(paramName.ToUpper(), paramValue);
                }
            }
            // End of the loop
            
            // Now, add any default parameters to the Dictionary, if not specified in the arguments
            string result;

            // Check for SignatureMethod
            if (!argParameters.TryGetValue("SIGNATUREMETHOD", out result))
            {
                argParameters.Add("SIGNATUREMETHOD", "HmacSHA256");
            }

            // Check for SignatureVersion
            if (!argParameters.TryGetValue("SIGNATUREVERSION", out result))
            {
                argParameters.Add("SIGNATUREVERSION", "2");
            }

            // Check for Action
            if (!argParameters.TryGetValue("ACTION", out result))
            {
                argParameters.Add("ACTION", "RequestReport");
            }


            // Spool out the dictionary
            // Console.WriteLine("Dictionary Below");
            // foreach (KeyValuePair<string, string> item in argParameters)
            // {
            //     Console.WriteLine("Dictionary Key: {0}, Value: {1}", item.Key, item.Value);
            // }
            // Console.WriteLine("End Dictionary");
            return argParameters;
        }


        // Tuple Returns Action Info in this order:
        // 1)  Version
        // 2)  Section
        public static (string, string) getActionInfo(string s)
        {
            switch (s)
            {
                // Reports:  Version 2009-01-01
                case "RequestReport":
                    return ("2009-01-01", "Reports");
                case "GetReportRequestList":
                    return ("2009-01-01", "Reports");
                case "GetReportRequestListByNextToken":
                    return ("2009-01-01", "Reports");
                case "GetReportRequestCount":
                    return ("2009-01-01", "Reports");
                case "CancelReportRequests":
                    return ("2009-01-01", "Reports");
                case "GetReportList":
                    return ("2009-01-01", "Reports");
                case "GetReportListByNextToken":
                    return ("2009-01-01", "Reports");
                case "GetReportCount":
                    return ("2009-01-01", "Reports");
                case "GetReport":
                    return ("2009-01-01", "Reports");
                case "ManageReportSchedule":
                    return ("2009-01-01", "Reports");
                case "GetReportScheduleList":
                    return ("2009-01-01", "Reports");
                case "GetReportScheduleListByNextToken":
                    return ("2009-01-01", "Reports");
                case "GetReportScheduleCount":
                    return ("2009-01-01", "Reports");
                case "UpdateReportAcknowledgements":
                    return ("2009-01-01", "Reports");

                // Orders:  Version 2013-09-01
                case "ListOrders":
                    return ("2013-09-01", "Orders");
                case "ListOrdersByNextToken":
                    return ("2013-09-01", "Orders");
                case "GetOrder":
                    return ("2013-09-01", "Orders");
                case "ListOrderItems":
                    return ("2013-09-01", "Orders");
                case "ListOrderItemsByNextToken":
                    return ("2013-09-01", "Orders");

                // Sellers:  Version 2011-07-01
                case "ListMarketplaceParticipations":
                    return ("2011-07-01", "Sellers");
                case "ListMarketplaceParticipationsByNextToken":
                    return ("2011-07-01", "Sellers");
                case "GetServiceStatus":
                    return ("2011-07-01", "Sellers");

                // Undefined Section Here
                default:
                    Console.WriteLine("Cannot find API Version for this action.  Please try a different action");
                    System.Environment.Exit(1);
                    return ("", "");
            }
        }
        
        public static string BuildURL(Dictionary<string, string> argParameters)
        {
            // We will do this with a multidimensional array.  First step, create the list that has all of the parameters
            List<string> parameterList = new List<string>();
            string action;
            string marketplaceUrl = argParameters["MARKETPLACEURL"];
            string awsSecretKey = argParameters["AWSSECRETKEY"];

            // string mwsAuthToken;
            // if (argParameters.TryGetValue("MWSAUTHTOKEN", out mwsAuthToken))
            // {
            //  Console.WriteLine("Added MWSAuthToken to List");
            //  Console.WriteLine(mwsAuthToken);
            //  Console.WriteLine("");
            // }

            if (argParameters.TryGetValue("ACTION", out action))
            {
                parameterList = UtilityClass.getParameters(action);
                // parameterList.ForEach(s => Console.WriteLine("{0}\t", s));
                // Console.WriteLine("");
                // Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Could not find the action to build the table!  Aborting!");
                // Console.ReadLine();
                System.Environment.Exit(1);
            }

            string version;
            string section;
            string timestamp;


            // Get the Version and Section
            (version, section) = getActionInfo(action);


            // Move the List to a Multidimensional Array
            string[,] parameterArray = new string[parameterList.Count, 4];
            int i = 0;

            foreach (string j in parameterList)
            {
                parameterArray[i, 0] = j;
                parameterArray[i, 2] = UtilityClass.StringToHex(j);

                string result;

                // Match the Argument with the parameterArray, ParameterName column
                if (argParameters.TryGetValue(j.ToUpper(), out result))
                {
                    parameterArray[i, 1] = result;
                    parameterArray[i, 3] = Uri.EscapeDataString(result);
                }


                // Populate the Seller ID
                /*
                if (j.ToUpper() == "SELLERID")
                {
                    parameterArray[i, 1] = argParameters["SELLERID"];
                    parameterArray[i, 2] = UtilityClass.StringToHex(j);
                    parameterArray[i, 3] = Uri.EscapeDataString(argParameters["SELLERID"]);
                }
                */

                // Populate the start/end dates
                if (j.ToUpper() == "STARTDATE")
                {
                    if (parameterArray[i, 1] != null)
                    {
                        parameterArray[i, 2] = UtilityClass.StringToHex(j);
                        parameterArray[i, 3] = Uri.EscapeDataString(translateDate(parameterArray[i, 1], true));
                    }
                }

                // Populate the start/end dates
                if (j.ToUpper() == "ENDDATE")
                {
                    if (parameterArray[i, 1] != null)
                    {
                        parameterArray[i, 2] = UtilityClass.StringToHex(j);
                        parameterArray[i, 3] = Uri.EscapeDataString(translateDate(parameterArray[i, 1], false));
                    }
                }

                // Populate the version
                if (j.ToUpper() == "VERSION")
                {
                    parameterArray[i, 1] = version;
                    parameterArray[i, 2] = UtilityClass.StringToHex(j);
                    parameterArray[i, 3] = Uri.EscapeDataString(version);
                }

                // Populate the timestamp
                if (j.ToUpper() == "TIMESTAMP")
                {
                    timestamp = UtilityClass.getTimeStamp();
                    parameterArray[i, 1] = timestamp;
                    parameterArray[i, 2] = UtilityClass.StringToHex(j);
                    parameterArray[i, 3] = Uri.EscapeDataString(timestamp);
                }

                // Populate MWS Auth Key, if not null
                //if (j.ToUpper() == "MWSAUTHTOKEN")
                //{
                //    parameterArray[i, 1] = mwsAuthToken;
                //    parameterArray[i, 2] = UtilityClass.StringToHex(j);
                //    parameterArray[i, 3] = Uri.EscapeDataString(version);
                //}


                // Populate the marketplaceUrl var
                if (j.ToUpper() == "MARKETPLACEURL")
                {
                    marketplaceUrl = parameterArray[i, 1];
                }


                // Console.WriteLine("{0}, {1}, {2}, {3}", parameterArray[i, 0], parameterArray[i, 1], parameterArray[i, 2], parameterArray[i, 3]);
                i++;
            }

            // Sort the parameters based on the Hexidecimal Key
            string[,] sortedParameterTable = parameterArray.OrderBy(x => x[2]);

            // This is the parameter section of the string to be signed
            string parameterSuffix = "";

            int counter = 1;
            for (i = 0; i < sortedParameterTable.GetLength(0); i++)
            {
                // Console.WriteLine("{0} = {1} = {2} = {3}", sortedParameterTable[i, 0], sortedParameterTable[i, 1], sortedParameterTable[i, 2], sortedParameterTable[i, 3]);

                if (sortedParameterTable[i, 3] != null)
                {
                    if (counter == 1)
                    {
                        parameterSuffix = parameterSuffix + sortedParameterTable[i, 0] + "=" + sortedParameterTable[i, 3];
                    }
                    else
                    {
                        parameterSuffix = parameterSuffix + "&" + sortedParameterTable[i, 0] + "=" + sortedParameterTable[i, 3];
                    }
                    counter++;
                }

            }

            // Create the header
            string headerString = "GET" + "\n" + marketplaceUrl + "\n" + "/" + section + "/" + version + "\n";
            // Console.WriteLine("Header to be signed: {0}", headerString);

            // Sign the string
            string signature = UtilityClass.GetSignature(headerString + parameterSuffix, awsSecretKey);
            // Signing this:
            // Console.WriteLine("Signature Line:");
            // Console.WriteLine(headerString + parameterSuffix);
            // Console.WriteLine("----------------------------------------------------");

            // Append Signature to Parameter Suffix
            parameterSuffix = parameterSuffix + "&Signature=" + Uri.EscapeDataString(signature);

            // URL Prefix
            string urlPrefix = "https://" + marketplaceUrl + "/" + section + "/" + version + "?";

            // Final URL
            string urlFinal = urlPrefix + parameterSuffix;
            // Console.WriteLine("");
            // Console.WriteLine("");
            // Console.WriteLine(urlFinal);
            // Console.ReadLine();
            return urlFinal;
        }


        public static string translateDate(string datetime, bool isRounded)
        {
            string strDays;
            Double dblDays;
            DateTime dateValue;
            string strDate;
            // Check for the lonely NOW parameter
            if (datetime.ToUpper() == "NOW")
            {
                if (isRounded)
                {
                    strDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    return strDate + "T00:00:00Z";
                }
                else
                {
                    return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }
            }
            // Next, check for the NOW parameter along with a value
            else if (datetime.ToUpper().StartsWith("NOW"))
            {
                strDays = datetime.Substring(3, datetime.Length - 3).Trim();
                if (Double.TryParse(strDays, out dblDays))
                {
                    if (isRounded)
                    {
                        strDate = DateTime.UtcNow.AddDays(dblDays).ToString("yyyy-MM-dd");
                        return strDate + "T00:00:00Z";
                    }
                    else
                    {
                        return DateTime.UtcNow.AddDays(dblDays).ToString("yyyy-MM-ddTHH:mm:ssZ");
                    }
                }
                else
                {
                    Console.WriteLine("Unable to parse the date with the NOW parameter.");
                    System.Environment.Exit(1);
                }
            }
            // Lastly, try to convert whatever date was provided to an understood date.
            else
            {
                if (DateTime.TryParse(datetime, out dateValue))
                {
                    return dateValue.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }
                else
                {
                    Console.WriteLine("Unable to parse the date with the Actual Date parameter.");
                    System.Environment.Exit(1);
                }
            }
            // Code should never reach this position
            return "1";
        }



        public static List<string> getParameters(string action)
        {
            List<string> parameter = new List<string>();

            // Next Add the Required Parameters
            parameter.Add("AWSAccessKeyId");
            parameter.Add("Action");
            parameter.Add("MWSAuthToken");
            parameter.Add("SellerId");
            parameter.Add("SignatureMethod");
            parameter.Add("SignatureVersion");
            parameter.Add("Timestamp");
            parameter.Add("Version");

            switch (action)
            {
                // Need to add all of the API Calls here
                // RequestReport
                case "RequestReport":
                    parameter.Add("EndDate");
                    parameter.Add("ReportOptions");
                    parameter.Add("ReportType");
                    parameter.Add("StartDate");
                    break;
                case "GetReportRequestList":
                    parameter.Add("ReportRequestIdList.Id.1");
                    parameter.Add("ReportTypeList");
                    parameter.Add("ReportProcessingStatusList");
                    parameter.Add("MaxCount");
                    parameter.Add("RequestedFromDate");
                    parameter.Add("RequestedToDate");
                    break;
                case "GetReportList":
                    parameter.Add("MaxCount");
                    parameter.Add("ReportTypeList");
                    parameter.Add("Acknowledged");
                    parameter.Add("ReportRequestIdList.Id.1");
                    parameter.Add("AvailableFromDate");
                    parameter.Add("AvailableToDate");
                    break;
                case "GetReport":
                    parameter.Add("ReportId");
                    break;
            }

            return parameter;
        }

        public static (string, string) getArgumentValue(string s)
        {
            // Init the variables
            string paramName = "";
            string paramValue = "";

            // Now, find where the = character is
            int i = s.IndexOf("=");

            // Therefore, paramName starts at char 2 (disregard the / char)
            // and ends at the = index-1

            paramName = s.Substring(1, i - 1);
            paramValue = s.Substring(i + 1, s.Length - i - 1);

            return (paramName, paramValue);
        }

        public static (string, string) getMarketplaceInfo(string s)
        {
            switch (s)
            {
                case "US":
                    return ("ATVPDKIKX0DER", "mws.amazonservices.com");
                case "CA":
                    return ("A2EUQ1WTGCTBG2", "mws.amazonservices.com");
                case "MX":
                    return ("A1AM78C64UM0Y8", "mws.amazonservices.com");
                case "ES":
                    return ("A1RKKUPIHCS9HS", "mws-eu.amazonservices.com");
                case "UK":
                    return ("A1F83G8C2ARO7P", "mws-eu.amazonservices.com");
                case "FR":
                    return ("A13V1IB3VIYZZH", "mws-eu.amazonservices.com");
                case "DE":
                    return ("A1PA6795UKMFR9", "mws-eu.amazonservices.com");
                case "IT":
                    return ("APJ6JRA9NG5V4", "mws-eu.amazonservices.com");
                case "BR":
                    return ("A2Q3Y263D00KWC", "mws.amazonservices.com");
                case "IN":
                    return ("A21TJRUUN4KGV", "mws.amazonservices.in");
                case "CN":
                    return ("AAHKV2X7AFYLW", "mws.amazonservices.com.cn");
                case "JP":
                    return ("A1VC38T7YXB528", "mws.amazonservices.jp");
                case "AU":
                    return ("A39IBJ37TRP1C6", "mws.amazonservices.com.au");
                default:
                    Console.WriteLine("CountryId parameter is invalid.  Please try again.");
                    System.Environment.Exit(1);
                    return ("", "");
            }
        }

        public static string UrlEncode(String data, bool path)
        {
            if (data == null)
            {
                return "";
            }
            StringBuilder encoded = new StringBuilder();
            String unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~" + (path ? "/" : "");

            foreach (char symbol in System.Text.Encoding.UTF8.GetBytes(data))
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                {
                    encoded.Append(symbol);
                }
                else
                {
                    encoded.Append("%" + String.Format("{0:X2}", (int)symbol));
                }
            }
            return encoded.ToString();
        }

        public static String GetSignature(String data, String key)
        {
            /*
            public static String GetSignature(String data, String key, KeyedHashAlgorithm algorithm)
            Encoding encoding = new UTF8Encoding();
            algorithm.Key = encoding.GetBytes(key);
            return Convert.ToBase64String(algorithm.ComputeHash(
                encoding.GetBytes(data.ToCharArray())));
            */

            UTF8Encoding encoding = new UTF8Encoding();
            HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(key));
            return Convert.ToBase64String(hmac.ComputeHash(encoding.GetBytes(data)));
        }

        public static string GetMyHash(string secretkey, string url, bool urlEncode)
        {
            byte[] key = new Byte[64];
            string b64 = null;
            key = Encoding.UTF8.GetBytes(secretkey);
            HMACSHA256 myhash256 = new HMACSHA256(key);
            byte[] urlbytes = Encoding.UTF8.GetBytes(url);            
            byte[] hashValue = myhash256.ComputeHash(urlbytes);
            b64 = Convert.ToBase64String(hashValue);
            
            if (urlEncode)
            {
                return Uri.EscapeUriString(b64);
                //return HttpUtility.UrlEncode(b64);
            }
            else
            {
                return b64;
            }
        }


        public static string GetResponse(string currentAction, string url)
        {
            
            try
            {
                // Finally, one that actually works!
                XDocument doc = XDocument.Load(url);
                string result="";

                switch (currentAction)
                {
                    case "RequestReport":
                        // Gets the ReportRequestId
                        result = doc.Descendants(XName.Get("ReportRequestId", @"http://mws.amazonaws.com/doc/2009-01-01/")).First().Value;
                        break;

                    case "GetReportRequestList":
                        result = doc.Descendants(XName.Get("ReportProcessingStatus", @"http://mws.amazonaws.com/doc/2009-01-01/")).First().Value;
                        break;

                    case "GetReportList":
                        result = doc.Descendants(XName.Get("ReportId", @"http://mws.amazonaws.com/doc/2009-01-01/")).First().Value;
                        break;
                }

                Console.WriteLine(result);
                return result;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Caught Exception: " + e.Message);
                System.Console.WriteLine("Stack Trace: " + e.StackTrace);
                System.Environment.Exit(1);
            }

            return null;
        }




        public static string getTimeStamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            // return "2018-07-04T05:57:09Z";
        }

        /// <summary>
        ///   Orders the two dimensional array by the provided key in the key selector.
        /// </summary>
        /// <typeparam name="T">The type of the source two-dimensional array.</typeparam>
        /// <param name="source">The source two-dimensional array.</param>
        /// <param name="keySelector">The selector to retrieve the column to sort on.</param>
        /// <returns>A new two dimensional array sorted on the key.</returns>
        public static T[,] OrderBy<T>(this T[,] source, Func<T[], T> keySelector)
        {
            return source.ConvertToSingleDimension().OrderBy(keySelector).ConvertToMultiDimensional();
        }
        /// <summary>
        ///   Orders the two dimensional array by the provided key in the key selector in descending order.
        /// </summary>
        /// <typeparam name="T">The type of the source two-dimensional array.</typeparam>
        /// <param name="source">The source two-dimensional array.</param>
        /// <param name="keySelector">The selector to retrieve the column to sort on.</param>
        /// <returns>A new two dimensional array sorted on the key.</returns>
        public static T[,] OrderByDescending<T>(this T[,] source, Func<T[], T> keySelector)
        {
            return source.ConvertToSingleDimension().
                OrderByDescending(keySelector).ConvertToMultiDimensional();
        }
        /// <summary>
        ///   Converts a two dimensional array to single dimensional array.
        /// </summary>
        /// <typeparam name="T">The type of the two dimensional array.</typeparam>
        /// <param name="source">The source two dimensional array.</param>
        /// <returns>The repackaged two dimensional array as a single dimension based on rows.</returns>
        private static IEnumerable<T[]> ConvertToSingleDimension<T>(this T[,] source)
        {
            T[] arRow;

            for (int row = 0; row < source.GetLength(0); ++row)
            {
                arRow = new T[source.GetLength(1)];

                for (int col = 0; col < source.GetLength(1); ++col)
                    arRow[col] = source[row, col];

                yield return arRow;
            }
        }
        /// <summary>
        ///   Converts a collection of rows from a two dimensional array back into a two dimensional array.
        /// </summary>
        /// <typeparam name="T">The type of the two dimensional array.</typeparam>
        /// <param name="source">The source collection of rows to convert.</param>
        /// <returns>The two dimensional array.</returns>
        private static T[,] ConvertToMultiDimensional<T>(this IEnumerable<T[]> source)
        {
            T[,] twoDimensional;
            T[][] arrayOfArray;
            int numberofColumns;

            arrayOfArray = source.ToArray();
            numberofColumns = (arrayOfArray.Length > 0) ? arrayOfArray[0].Length : 0;
            twoDimensional = new T[arrayOfArray.Length, numberofColumns];

            for (int row = 0; row < arrayOfArray.GetLength(0); ++row)
                for (int col = 0; col < numberofColumns; ++col)
                    twoDimensional[row, col] = arrayOfArray[row][col];

            return twoDimensional;
        }
    }
}
