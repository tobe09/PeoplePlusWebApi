using System;
using System.Data;
using System.Web.Http;

namespace PeoplePlusWebApi.Controllers
{
    public class MedicalRequestController : ApiController
    {
        public DataProvider.MedicalRequest MedReqObj
        {
            get { return new DataProvider.MedicalRequest(); }
        }

        public object GetHospitals(string compId)
        {
            try
            {
                DataTable hospitalDt = MedReqObj.GetHospitals(compId);
                var response = new { Values = hospitalDt };
                return response;
            }

            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        public object PostRequest([FromBody] MedRequestClass requestObj)
        {
            try
            {
                string startdate = requestObj.DateStr;
                DateTime? startDt = new CommonMethodsClass().ConvertStringToDate(startdate);
                if (DateTime.Now >= startDt || startDt == null)
                {
                    return new { Error = "Request date must be greater than today's date" };
                }

                object response;
                DataTable lastNumDt = MedReqObj.GetLastReqNo(requestObj.CompId);
                int newNo = int.Parse(lastNumDt.Rows[0]["MAXVAL"].ToString()) + 1;
                string newReqNo = "RQ" + new CommonMethodsClass().ZeroPad(newNo + "");

                object[] reqInsertPara = new object[9];
                reqInsertPara[0] = newReqNo;
                reqInsertPara[1] = requestObj.EmployeeNo;
                reqInsertPara[2] = requestObj.HospitalId;
                reqInsertPara[3] = DateTime.Parse(requestObj.DateStr).ToString("dd-MMM-yyyy");
                reqInsertPara[4] = requestObj.Reason.ToUpper();
                reqInsertPara[5] = "R";
                reqInsertPara[6] = "";
                reqInsertPara[7] = requestObj.UserId;
                reqInsertPara[8] = requestObj.CompId;

                int resultInt = DataCreator.ExecuteProcedure("insert_h_mdcl_rsqt", reqInsertPara);

                if (resultInt == 0)
                {
                    DataTable medReqIdDt = MedReqObj.GetMedicalReqId(requestObj.CompId, newReqNo, requestObj.EmployeeNo);
                    if (medReqIdDt.Rows.Count != 0)
                    {
                        int requestId = int.Parse(medReqIdDt.Rows[0]["VAL"].ToString());

                        object[] reqProc = new object[5];
                        reqProc[0] = requestObj.CompId;
                        reqProc[1] = "MDR";
                        reqProc[2] = requestId;
                        reqProc[3] = requestObj.EmployeeNo;
                        reqProc[4] = 1;

                        resultInt = DataCreator.ExecuteProcedure("REQUEST_PROCESS", reqProc);

                        if (resultInt != 0)
                        {
                            string delQuery = "DELETE H_MDCL_RSQT WHERE COMP_AID='" + requestObj.CompId + "' AND RQST_ID=" + requestId;
                            DataCreator.ExecuteSQL(delQuery);
                        }
                        else
                        {
                            DataTable medReqApprDt = MedReqObj.GetMedicalRequestAppraisal(requestObj.CompId, requestId + "");

                            if (medReqApprDt.Rows.Count > 0)
                            {
                                for (int i = 0; i < medReqApprDt.Rows.Count; i++)
                                {
                                    int intApprEmpID = DBNull.Value.Equals(medReqApprDt.Rows[i]["appr_emp_aid"]) ? 0 : int.Parse(medReqApprDt.Rows[i]["appr_emp_aid"].ToString());
                                    string strApprovrEmail = DBNull.Value.Equals(medReqApprDt.Rows[i]["apprvr_email"]) ? "NILEBANKHR" : medReqApprDt.Rows[i]["apprvr_email"].ToString();
                                    string strRqstEmp = DBNull.Value.Equals(medReqApprDt.Rows[i]["requester"]) ? "" : medReqApprDt.Rows[i]["requester"].ToString().Trim();
                                    string strSubject = "Medical";
                                    string messageBody = strRqstEmp + "'s " + strSubject +
                                        " request awaits your attention, kindly visit the Self Service Medical Approval page to handle request";

                                    if (strApprovrEmail != "")
                                    {
                                        new CommonMethodsClass().MailRoutines(strApprovrEmail, strSubject, messageBody, requestObj.CompId);
                                    }

                                    new NotificationController().SendNotification(requestObj.CompId, requestId, intApprEmpID, requestObj.UserId, strSubject,
                                        strRqstEmp + "'s " + strSubject + " request awaits your attention. Kindly visit the WorkListViewer to approve it");

                                    new CommonMethodsClass().SendAlerts(requestObj.CompId, requestObj.UserId, intApprEmpID, messageBody, "Approvals", 16, requestId);
                                }

                            }
                        }
                    }
                    else
                        resultInt = 1;      //error condition
                }

                string error;
                if (resultInt == 0) error = "";
                else error = "Error Saving Records";    //"Not Provisioned For User";

                response = new { Error = error };

                return response;
            }

            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }
        

        public class MedRequestClass
        {
            public int EmployeeNo { get; set; }
            public string HospitalId { get; set; }
            public string DateStr { get; set; }
            public string Reason { get; set; }
            public string UserId { get; set; }
            public string CompId { get; set; }
        }

    }
}
