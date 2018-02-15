using System;
using System.Data;
using System.Web.Http;
using System.Net.Http;

namespace PeoplePlusWebApi.Controllers
{
    public class ApprovalController : ApiController
    {
        private DataProvider.Approval _apprObj = new DataProvider.Approval();
        public DataProvider.Approval ApprObj
        {
            get { return _apprObj; }
        }

        public object GetWorkListViewer(string compId, int empNo)
        {
            try
            {
                object response;

                DataTable workListDt = ApprObj.GetWorkListViewer(compId, empNo);

                if (workListDt == null)
                {
                    response = new { Error = "Error Loading records" };
                }
                else if (workListDt.Rows.Count <= 0)
                {
                    response = new { Error = "No Task Available" };
                }
                else
                {
                    response = new { Values = workListDt, Error = "" };
                }
                
                return Request.CreateResponse(System.Net.HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        [Route("api/Approval/GetCount")]
        public object GetWorkListCount(string compId, int empNo)
        {
            try
            {
                object response;

                DataTable workListDt = ApprObj.GetWorkListViewer(compId, empNo);

                if (workListDt == null)
                {
                    response = new { Count = 0, Error = "Error Loading record" };
                }
                else if (workListDt.Rows.Count <= 0)
                {
                    response = new { Count = 0, Error = "No Task Available" };
                }
                else
                {
                    response = new { Count = workListDt.Rows.Count, Error = "" };
                }

                return response;
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }

        }

        [Route("api/Approval/GetMedicalRequest")]
        public object GetMedicalRequest(string compId, string empNo, string accYear, string reqId)
        {
            try
            {
                object response;

                DataTable medValidateDt = ApprObj.MedApprovalValidate(compId, empNo);

                if (medValidateDt.Rows.Count > 0)
                {
                    DataTable medRequestDt = ApprObj.MedApprovalRequest(compId, accYear, reqId);

                    if (medRequestDt == null)
                    {
                        response = new { Error = "Error Loading records" };
                    }
                    else if (medRequestDt.Rows.Count <= 0)
                    {
                        response = new { Error = "Expired Medical Request (No data available for this request)" };
                    }
                    else
                    {
                        response = new { Values = medRequestDt, Error = "" };
                    }
                }
                else
                {
                    response = new { Error = "You do not have access to this record" };
                }

                return response;
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        [Route("api/Approval/GetLeaveRequest")]
        public object GetLeaveRequest(string compId, string empNo, string reqId)
        {
            try
            {
                object response;

                DataTable lvlValidateDt = ApprObj.LvlApprovalValidate(compId, empNo);

                if (lvlValidateDt.Rows.Count > 0)
                {

                    DataTable lvlRequestDt = ApprObj.LvlApprovalRequest(compId, reqId);

                    if (lvlRequestDt == null)
                    {
                        response = new { Error = "Error Loading records" };
                    }
                    else if (lvlRequestDt.Rows.Count <= 0)
                    {
                        response = new { Error = "Expired Leave Request (No data available for this request)" };
                    }
                    else
                    {
                        response = new { Values = lvlRequestDt, Error = "" };
                    }
                }
                else
                {
                    response = new { Error = "You do not have access to this record" };
                }

                return response;
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        [Route("api/Approval/GetTrainingRequest")]
        public object GetTrainingRequest(string compId, string empNo, string reqId)
        {
            try
            {
                object response;

                DataTable trnValidateDt = ApprObj.TrnApprovalValidate(compId, empNo);

                if (trnValidateDt.Rows.Count > 0)
                {
                    DataTable trnRequestDt = ApprObj.TrnApprovalRequest(compId, reqId);

                    if (trnRequestDt == null)
                    {
                        response = new { Error = "Error Loading records" };
                    }
                    else if (trnRequestDt.Rows.Count <= 0)
                    {
                        response = new { Error = "Expired Training Request (No data available for this request)" };
                    }
                    else
                    {
                        response = new { Values = trnRequestDt, Error = "" };
                    }
                }
                else
                {
                    response = new { Error = "You do not have access to this record" };
                }

                return response;
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        public object PostApproval([FromBody] ApprovalClass approvalObj)
        {
            try
            {
                object response;

                string error;
                if (approvalObj.RequestType == "Medical")
                {
                    approvalObj.RequestType = "MDR";
                    error = UpdateMedical(approvalObj);
                }
                else if (approvalObj.RequestType == "Leave")
                {
                    approvalObj.RequestType = "LVR";
                    error = UpdateLeave(approvalObj);
                }
                else if (approvalObj.RequestType == "Training")
                {
                    approvalObj.RequestType = "TRN";
                    error = UpdateTraining(approvalObj);
                }
                else
                {
                    error = "Request type not available";
                }

                response = new { Error = error };

                return Request.CreateResponse(System.Net.HttpStatusCode.OK, response);          //issues with asynchrony
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        [NonAction]
        public string UpdateMedical(ApprovalClass apprObj)
        {
            string strError;

            object[] updatePara = new object[9];
            updatePara[0] = apprObj.CompanyCode;
            updatePara[1] = apprObj.RequestNo;
            updatePara[2] = apprObj.ApproveeNo;
            updatePara[3] = apprObj.Status;
            updatePara[4] = apprObj.RequestId;
            updatePara[5] = apprObj.RequestType;
            updatePara[6] = apprObj.Reason;
            updatePara[7] = apprObj.EmployeeNo;
            updatePara[8] = apprObj.UserName;

            int status = DataCreator.ExecuteProcedure("UPDATE_SS_MEDAPPROVAL", updatePara);

            if (status == 0)
            {
                DataTable statusDt = ApprObj.MedApprovalStatus(apprObj.CompanyCode, apprObj.RequestId, apprObj.ApproveeNo);

                string strStatus = "";
                int currentStage = 0;

                if (statusDt.Rows.Count > 0)
                {
                    strStatus = DBNull.Value.Equals(statusDt.Rows[0]["REQ_STATUS"]) ? "" : (string)statusDt.Rows[0]["REQ_STATUS"];
                    currentStage = DBNull.Value.Equals(statusDt.Rows[0]["RQST_CUR_STAGE"]) ? 0 : int.Parse(statusDt.Rows[0]["RQST_CUR_STAGE"].ToString());

                    DismissPreviousAlerts(apprObj);

                    if (strStatus == "U" || strStatus == "D")
                    {
                        string query = " DELETE SS_APPROVAL_EMPLOYEES WHERE COMP_AID ='" + apprObj.CompanyCode + "' and RQST_ID =" + apprObj.RequestId;
                        DataCreator.ExecuteSQL(query);
                    }
                    else
                    {
                        status = UpdateRequestProcess(apprObj, currentStage);

                        if (status == 0)
                        {
                            DataTable validatorsDt = ApprObj.MedApprovalValidators(apprObj.CompanyCode, apprObj.RequestId);
                            SendMailAndAlert(apprObj, validatorsDt, "Medical");
                        }
                    }
                }
            }
            else
                status = 1;

            if (status == 0)
            {
                new CommonMethodsClass().DismissAlert(apprObj.CompanyCode, apprObj.UserName, apprObj.AlertId, apprObj.RequestId);
                strError = "";
            }
            else
            {
                strError = "Error Saving Record";
            }

            return strError;
        }

        [NonAction]
        public string UpdateLeave(ApprovalClass apprObj)
        {
            string strError;

            object[] updatePara = new object[9];
            updatePara[0] = apprObj.CompanyCode;
            updatePara[1] = apprObj.RequestNo;
            updatePara[2] = apprObj.ApproveeNo;
            updatePara[3] = apprObj.Status;
            updatePara[4] = apprObj.RequestId;
            updatePara[5] = apprObj.RequestType;
            updatePara[6] = apprObj.Reason;
            updatePara[7] = apprObj.EmployeeNo;
            updatePara[8] = apprObj.UserName;

            int status = DataCreator.ExecuteProcedure("UPDATE_SS_LVAPPROVAL", updatePara);

            if (status == 0)
            {
                object[] modifyPara = new object[5];
                modifyPara[0] = apprObj.CompanyCode;
                modifyPara[1] = apprObj.RequestNo;
                modifyPara[2] = apprObj.ProcessFrom;
                modifyPara[3] = apprObj.ProcessTo;
                modifyPara[4] = apprObj.DayDiff;

                status = DataCreator.ExecuteProcedure("UPDATE_SS_REQ_APPROVAL", modifyPara);

                DataTable leaveCurrStage = ApprObj.GetLeaveCurrentStage(apprObj.CompanyCode, apprObj.RequestId, apprObj.ApproveeNo);

                if (leaveCurrStage.Rows.Count > 0)
                {
                    string strStatus = DBNull.Value.Equals(leaveCurrStage.Rows[0]["LV_STS"]) ? "" : (string)leaveCurrStage.Rows[0]["LV_STS"];
                    int currentStage = DBNull.Value.Equals(leaveCurrStage.Rows[0]["RQST_CUR_STAGE"]) ? 0 : int.Parse(leaveCurrStage.Rows[0]["RQST_CUR_STAGE"].ToString());

                    DismissPreviousAlerts(apprObj);

                    if (strStatus == "U" || strStatus == "D")
                    {
                        string query = "DELETE SS_APPROVAL_EMPLOYEES WHERE COMP_AID ='" + apprObj.CompanyCode + "' and RQST_ID =" + apprObj.RequestId;
                        DataCreator.ExecuteSQL(query);
                    }
                    else
                    {
                        status = UpdateRequestProcess(apprObj, currentStage);

                        if (status == 0)
                        {
                            DataTable leaveApprStage1Dt = ApprObj.GetLeaveApprovalStage1(apprObj.CompanyCode, apprObj.RequestId);
                            if (leaveApprStage1Dt.Rows.Count > 0)
                            {
                                SendMailAndAlert(apprObj, leaveApprStage1Dt, "Leave");
                            }
                        }
                    }

                    //This Code Segment Handles The Alert To Be Sent To The Initiator Of The Process On The Progress Of This Process
                    DataTable informSelfDt = ApprObj.LeaveApprovalSelfMe(apprObj.CompanyCode, apprObj.ApproveeNo);
                    string recieverMailAdr = "NEPTUNEHR";
                    if (informSelfDt.Rows.Count > 0)
                    {
                        recieverMailAdr = DBNull.Value.Equals(informSelfDt.Rows[0]["apprvr_email"]) ? "NEPTUNEHR" : (string)informSelfDt.Rows[0]["apprvr_email"];
                    }
                    //Sends Alert and Mail To The Applicant
                    if (strStatus == "U")
                    {
                        //remove duplicate requests for extensions
                        string updateLvRqst = "UPDATE   H_EMP_LV_RQST SET LV_STS = 'D' WHERE H_RQSN_NMBR IN(SELECT H_RQST_NMBR FROM H_EMP_LV_EXTENSION WHERE  " +
                            " RQST_ID = " + apprObj.RequestId + ")";
                        DataCreator.ExecuteSQL(updateLvRqst);

                        string message = "Your Leave Request Has Been Approved For " + apprObj.DayDiff + " Day(s). To Commence On " + apprObj.DayStart + " And Ends On " +
                            apprObj.DayEnd + ". Your Resumption Date Is On " + DateTime.Parse(apprObj.DayEnd).Add(new TimeSpan(1, 0, 0, 0)).ToShortDateString() + "";

                        new CommonMethodsClass().SendAlerts(apprObj.CompanyCode, apprObj.UserName, apprObj.ApproveeNo, message, "Approvals", 12, apprObj.RequestId, false);
                        new CommonMethodsClass().MailRoutines(recieverMailAdr, "Leave", message, apprObj.CompanyCode);
                    }
                    else if (strStatus == "D")
                    {
                        string message = "Your Leave Rquest Has Been Denied, kindly visit the Self Service Leave Monitor Page To View The Detail.";

                        new CommonMethodsClass().SendAlerts(apprObj.CompanyCode, apprObj.UserName, apprObj.ApproveeNo, message, "Approvals", 12, apprObj.RequestId, false);
                        new CommonMethodsClass().MailRoutines(recieverMailAdr, "Leave", message, apprObj.CompanyCode);
                    }
                    else
                    {
                        string message = "Your Leave request Has Moved To The Next Approval Stage, kindly visit the Self Service Leave Monitor page To View The Detail";

                        new CommonMethodsClass().SendAlerts(apprObj.CompanyCode, apprObj.UserName, apprObj.ApproveeNo, message, "Approvals", 12, apprObj.RequestId, false);
                    }

                }
                else
                    status = 1;     //error has occurred
            }

            if (status == 0)
            {
                new CommonMethodsClass().DismissAlert(apprObj.CompanyCode, apprObj.UserName, apprObj.AlertId, apprObj.RequestId);
                strError = "";
            }
            else
            {
                strError = "Error Saving Record";
            }

            return strError;
        }

        [NonAction]
        public string UpdateTraining(ApprovalClass apprObj)
        {
            string strError;

            object[] updatePara = new object[11];
            updatePara[0] = apprObj.CompanyCode;
            updatePara[1] = apprObj.ProgramCode;
            updatePara[2] = apprObj.SerialNo;
            updatePara[3] = apprObj.ApproveeNo;
            updatePara[4] = apprObj.Status;
            updatePara[5] = apprObj.ModifiedDate;
            updatePara[6] = apprObj.RequestType;
            updatePara[7] = apprObj.Reason;
            updatePara[8] = apprObj.EmployeeNo;
            updatePara[9] = apprObj.UserName;
            updatePara[10] = apprObj.RequestId;

            int status = DataCreator.ExecuteProcedure("UPDATE_SS_TRANAPPROVAL", updatePara);

            if (status == 0)
            {
                DataTable currentStageDt = ApprObj.GetTrainiingCurrentStage(apprObj.CompanyCode, apprObj.RequestId);
                if (currentStageDt.Rows.Count > 0)
                {
                    string strStatus = DBNull.Value.Equals(currentStageDt.Rows[0]["HTNM_STS"]) ? "" : (string)currentStageDt.Rows[0]["HTNM_STS"];
                    int currentStage = DBNull.Value.Equals(currentStageDt.Rows[0]["RQST_CUR_STAGE"]) ? 0 : int.Parse(currentStageDt.Rows[0]["RQST_CUR_STAGE"].ToString());

                    DismissPreviousAlerts(apprObj);

                    if (strStatus == "U" || strStatus == "D")
                    {
                        string query = " DELETE SS_APPROVAL_EMPLOYEES WHERE COMP_AID ='" + apprObj.CompanyCode + "' and RQST_ID =" + apprObj.RequestId;
                        DataCreator.ExecuteSQL(query);
                    }
                    else
                    {
                        status = UpdateRequestProcess(apprObj, currentStage);

                        if (status == 0)
                        {
                            DataTable ssGrieveGlDt = ApprObj.SelectGrieveGl(apprObj.CompanyCode, apprObj.RequestId);
                            if (ssGrieveGlDt.Rows.Count > 0)
                            {
                                SendMailAndAlert(apprObj, ssGrieveGlDt, "Training");
                            }
                        }
                    }
                }
            }
            else
                status = 1;

            if (status == 0)
            {
                new CommonMethodsClass().DismissAlert(apprObj.CompanyCode, apprObj.UserName, apprObj.AlertId, apprObj.RequestId);
                strError = "";
            }
            else
            {
                strError = "Error Saving Record";
            }

            return strError;
        }

        [NonAction]
        public void DismissPreviousAlerts(ApprovalClass apprObj)
        {

            string updateQuery = "UPDATE GM_ALERTS SET ALERT_STATUS = 'D' WHERE RQST_ID = '" + apprObj.RequestId + "'";
            DataCreator.ExecuteSQL(updateQuery);

            updateQuery = "UPDATE NOTIF_MSG_TEMP SET STATUS = 1, MDFD_BY = '" + apprObj.UserName + "', MDFD_DATE = SYSDATE WHERE MSG_RQST_ID = " +
                apprObj.RequestId + " AND COMP_AID = '" + apprObj.CompanyCode + "' AND STATUS=0";
            DataCreator.ExecuteSQL(updateQuery);
        }

        [NonAction]
        public int UpdateRequestProcess(ApprovalClass apprObj, int currentStage)
        {
            object[] varEnginePara = new object[5];
            varEnginePara[0] = apprObj.CompanyCode;
            varEnginePara[1] = apprObj.RequestType;
            varEnginePara[2] = apprObj.RequestId;
            varEnginePara[3] = apprObj.ApproveeNo;
            varEnginePara[4] = currentStage;

            int result = DataCreator.ExecuteProcedure("REQUEST_PROCESS", varEnginePara);
            return result;
        }

        [NonAction]
        public void SendMailAndAlert(ApprovalClass apprObj, DataTable appraisersDt, string subject)
        {
            if (appraisersDt.Rows.Count > 0)
            {
                int formId;
                if (subject.ToLower() == "medical") formId = 16;
                else if (subject.ToLower() == "leave") formId = 12;
                else if (subject.ToLower() == "training") formId = 27;
                else formId = 99;           //error condition

                for (int i = 0; i < appraisersDt.Rows.Count; i++)
                {
                    string appraiserName = DBNull.Value.Equals(appraisersDt.Rows[i]["requester"]) ? "" : (appraisersDt.Rows[i]["requester"] as string).Trim();
                    int recipientId = DBNull.Value.Equals(appraisersDt.Rows[i]["appr_emp_aid"]) ? 0 : int.Parse(appraisersDt.Rows[i]["appr_emp_aid"].ToString());
                    string message = appraiserName + "'s " + subject + " request awaits your attention, kindly visit the Self Service " + subject +
                                    " approval page to approve it";

                    string approverEmail = DBNull.Value.Equals(appraisersDt.Rows[i]["apprvr_email"]) ? "" : appraisersDt.Rows[i]["apprvr_email"] as string;
                    if (!string.IsNullOrEmpty(approverEmail))
                    {
                        new CommonMethodsClass().MailRoutines(approverEmail, subject, message, apprObj.CompanyCode);
                    }

                    new NotificationController().SendNotification(apprObj.CompanyCode, apprObj.RequestId, recipientId, apprObj.UserName, 
                        subject, appraiserName + "'s " + subject + " request awaits your attention. Kindly visit the WorkListViewer to approve it");

                    new CommonMethodsClass().SendAlerts(apprObj.CompanyCode, apprObj.UserName, recipientId, message, "APPROVALS", formId, apprObj.RequestId);
                }
            }
        }
    }


    public class ApprovalClass
    {
        public int EmployeeNo { get; set; }
        public int RequestId { get; set; }
        public string Reason { get; set; }
        public string UserName { get; set; }
        public string CompanyCode { get; set; }
        public int ApproveeNo { get; set; }
        public string Status { get; set; }
        public int AlertId { get; set; }
        public string RequestType { get; set; }

        //medical and leave
        public string RequestNo { get; set; }
        //training equivalent of RequestNo
        public int SerialNo { get; set; }

        //leave
        public string ProcessFrom { get; set; }
        public string ProcessTo { get; set; }
        public string DayStart { get; set; }
        public string DayEnd { get; set; }
        public int DayDiff { get; set; }

        //training
        public string ProgramCode { get; set; }
        public string ModifiedDate { get { return DateTime.Now.ToString("dd-MMM-yyyy"); } }
    }

}
