using System;
using System.Web.Http;

namespace PeoplePlusWebApi.Controllers
{
    public class AbsenteeismController : ApiController
    {
        public object PostRequest([FromBody] AbsenteeismReq requestObj)
        {
            try
            {
                string startdate = requestObj.DateStr;
                DateTime? startDt = new CommonMethodsClass().ConvertStringToDate(startdate);
                if (DateTime.Now >= startDt || startDt == null)
                {
                    return new { Error = "Request date must be greater than today's date" };
                }

                object[] reqInsertPara = new object[5];

              //  DateTime myDate = DateTime.Parse(requestObj.DateStr).ToString("dd-MMM-yyyy");
                
                reqInsertPara[0] = int.Parse(requestObj.EmployeeNo);
                reqInsertPara[1] = DateTime.Parse(requestObj.DateStr).ToString("dd-mmm-yyyy");
                reqInsertPara[2] = requestObj.Reason.ToUpper();
                reqInsertPara[3] = requestObj.UserId;
                reqInsertPara[4] = requestObj.CompId;

                int resultInt = DataCreator.ExecuteProcedure("INSERT_H_EMPLYE_ABSENTEESM", reqInsertPara);
                              
                string error;
                if (resultInt == 0) error = "";
                else error = "Error Saving Records";

                var response = new { Error = error };
                return response;
            }

            catch (Exception ex)
            {
                ex.Log();
                throw;
            }
        }
    }

    public class AbsenteeismReq
    {
        public string EmployeeNo { get; set; }
        public string DateStr { get; set; }
        public string Reason { get; set; }
        public string UserId { get; set; }
        public string CompId { get; set; }
    }
}
