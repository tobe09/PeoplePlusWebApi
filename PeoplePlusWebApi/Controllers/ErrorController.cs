using System;
using System.Web.Http;
using System.IO;

namespace PeoplePlusWebApi
{
    public class ErrorController : ApiController
    {
        public object PostError([FromBody]ErrorClass errorObj, string logFile = "ConsumerErrorLog.txt")
        {
            try
            {
                bool status;
                string path = Values.RootAddress + "App_Data\\" + logFile;

                string lastNumber = LastExceptionNumber(path);

                if (lastNumber != "Error")
                {
                    string newNumber = (int.Parse(lastNumber) + 1) + "";
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        string errorMessage = "\r\n\r\nDate: " + DateTime.Now.ToLongDateString() + "\r\nTime: " + DateTime.Now.ToLongTimeString() +
                                              "\r\nError message: " + errorObj.Message +
                                              "\r\nError's stack trace: " + errorObj.StackTrace +
                                              "\r\n#########################################################################################\r\n\r\n\r\n" +
                                              "Exception " + newNumber;
                        sw.Write(errorMessage);
                        sw.Flush();
                    }
                    status = true;
                }
                else
                {
                    status = false;
                }

                var response = new { Status = status };                     //send the encoded response as an anonymous object

                return response;
            }

            catch
            {
                throw new ExternalException();
            }
        }
        
        [NonAction]
        public static string LastExceptionNumber(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, "Exception 1");
                }
                string allText = File.ReadAllText(path);
                //check if value exists and get the last set of values
                string values = allText.Length < 10 ? "1" : allText.Substring(allText.Length-10);
                //split values to get the last number
                string[] valuesArray = values.Split(' ');
                //get the last number
                string lastNumber = valuesArray[valuesArray.Length - 1];
                int outNumber;
                //check if the last number is an integer
                lastNumber = int.TryParse(lastNumber, out outNumber) ? lastNumber : "1";
                return lastNumber;
            }
            catch
            {
                return "Error";
            }
        }
    }

    
    //static class extending the Log method for all extensions in namespace
    public static class InternalError
    {
        //extension method for exception logging at the server
        public static bool Log(this Exception ex)
        {
            try
            {
                ErrorClass err = new ErrorClass();
                err.Message = ex.Message;
                err.StackTrace = ex.StackTrace;
                dynamic status = new ErrorController().PostError(err, "InterenalErrorLog.txt");

                return status.Status;
            }
            catch 
            {
                return false;
            }
        }
    }


    //class modelling the information of a standard exception
    public class ErrorClass 
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }


    //formatted exception class for external consumption
    public class ExternalException : Exception
    {
        public override string Message { get; }
        public override string StackTrace { get { return "Protected"; } }

        public ExternalException()
        {
            Message = Values.ErroMsg;
        }

        public ExternalException(string message)
        {
            Message = message;
        }
    }
}
