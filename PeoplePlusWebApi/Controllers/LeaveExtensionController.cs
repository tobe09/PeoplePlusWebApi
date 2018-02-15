using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using System.Net.Mail;


namespace PeoplePlusWebApi.Controllers
{
    public class LeaveExtensionController : ApiController
    {
        private DataProvider.LeaveExtension _leaveExtObj = new DataProvider.LeaveExtension();
        public DataProvider.LeaveExtension LeaveExtObj { get { return _leaveExtObj; } }

        public object GetLeaveExt(string compId, int EmployeeNo)
        {
            DataTable LeaveType = LeaveExtObj.GetLeaveExt(compId, EmployeeNo);
            var response = new { Values = LeaveType };
            return response;
        }

        public object GetLeaveExtInfo(string compId, string RqstId, int EmployeeNo)
        {
            DataTable LeaveTypeInfo = LeaveExtObj.GetLeaveExtInfo(compId, EmployeeNo, RqstId);
            var response = new { Values = LeaveTypeInfo };
            return response;
        }

        public int GetWorkDays(string strFromDate, string strToDate, string compId)
        {
            return new CommonMethodsClass().GetWorkDays(strFromDate, strToDate, compId);
        }
        
        public object PostRequest([FromBody] LeaveExtClass extObj)
        {
            string startdate = extObj.ToDate;
            DateTime? startDt = new CommonMethodsClass().ConvertStringToDate(startdate);
            if (DateTime.Now >= startDt || startDt == null)
            {
                return new { Error = "Extended date must be greater than today's date" };
            }

            DataTable otherInfoDt = LeaveExtObj.GetOtherRequestInfo(extObj.CompId, extObj.PrevReqId);

            LeaveRequestController rqstContrlObj = new LeaveRequestController();
            var lvlReqObj = new LeaveRequestController.LeaveReqClass();

            lvlReqObj.AccountingYear = extObj.AccountingYear;
            lvlReqObj.Allowance = DBNull.Value.Equals(otherInfoDt.Rows[0]["RQS_LV_ALLW"]) ? "" : otherInfoDt.Rows[0]["RQS_LV_ALLW"].ToString();
            lvlReqObj.CompId = extObj.CompId;
            lvlReqObj.Duration = extObj.Duration;
            lvlReqObj.EmployeeNo = extObj.EmployeeNo;
            lvlReqObj.EnddateStr = extObj.ToDate;
            lvlReqObj.LvCode = extObj.LvCode;
            lvlReqObj.reason = extObj.Reason;
            lvlReqObj.ReliefOfficer= DBNull.Value.Equals(otherInfoDt.Rows[0]["LV_REL_OFF_NAME"]) ? "" : otherInfoDt.Rows[0]["LV_REL_OFF_NAME"].ToString();
            lvlReqObj.ReliefOfficerID = extObj.RelOfficerId;
            lvlReqObj.RemainingDays = extObj.RemainingDays;
            string strtDate= DBNull.Value.Equals(otherInfoDt.Rows[0]["P_DATE_FROM"]) ? "" : otherInfoDt.Rows[0]["P_DATE_FROM"].ToString();
            strtDate = DateTime.Parse(strtDate).ToString("dd/MM/yyyy");
            lvlReqObj.StartdateStr = strtDate;
            lvlReqObj.UserId = extObj.UserId;
            
            int newRqstId = 0;
            rqstContrlObj.NewRequestIdEvent += (s, intEv) => 
            {
                newRqstId = intEv;
            };

            dynamic status = rqstContrlObj.PostRequest(lvlReqObj, true);

            if (status.Error == "")
            {
                DataTable dv2 = LeaveExtObj.LeaveExtNum(extObj.CompId);

                int eReqNo;
                if (dv2.Rows.Count > 0)
                {
                    if (dv2.Rows[0]["ReqNum"] != DBNull.Value)
                        eReqNo = int.Parse(dv2.Rows[0]["ReqNum"].ToString()) + 1;
                    else
                        eReqNo = 1;
                }
                else
                    eReqNo = 1;

                object[] varInsertPara = new object[14];
                varInsertPara[0] = eReqNo;
                varInsertPara[1] = (int)double.Parse(extObj.ReqNo);
                varInsertPara[2] = int.Parse(extObj.EmployeeNo);
                varInsertPara[3] = extObj.AccountingYear;
                varInsertPara[4] = (int)double.Parse(extObj.LeaveGrp);
                varInsertPara[5] = extObj.LvCode;
                varInsertPara[6] = int.Parse(extObj.RemainingDays);
                varInsertPara[7] = int.Parse(extObj.Duration);
                string[] ToDate = extObj.ToDate.Split('/');
                varInsertPara[8] = new DateTime(int.Parse(ToDate[2]), int.Parse(ToDate[1]), int.Parse(ToDate[0])).ToString("dd-MMM-yyyy");
                varInsertPara[9] = "E";
                varInsertPara[10] = extObj.Reason;
                varInsertPara[11] = extObj.UserId;
                varInsertPara[12] = extObj.CompId;
                varInsertPara[13] = newRqstId;

                int result = DataCreator.ExecuteProcedure("INSERT_H_EMP_LV_Extension2", varInsertPara);

                if (result != 0)
                    status.Error = "Error Saving Extension Records";
            }

            return status;
        }

        //public object PostRequest([FromBody] LeaveExtClass ExtObj)
        //{
        //    object[] varAnnualLv = new object[6];
        //    object[] varCasualLv = new object[6];
        //    object[] varInsertPara = new object[13];
        //    object[] varInsertRemainData = new object[7];
        //    object[] varModifyPara = new object[13];
        //    object[] varEnginePara = new object[5];


        //    int intRqstID;
        //    int IntResult;
        //    int IntResultRow;
        //    string strApprovrEmail, strRqstrEmail, strRqstEmp, strSubject;
        //    int eReqNo;
        //    int intRes;
        //    int intApprEmpID;
        //    string StrQuery;

        //    try
        //    {              
        //        //'Select Maximun Request Number And Increment For this Request
        //        DataTable dv2 = LeaveExtObj.LeaveExtNum(ExtObj.CompId);
        //        if (dv2.Rows.Count > 0)
        //        {
        //            if (dv2.Rows[0]["ReqNum"] != System.DBNull.Value)
        //            {
        //                eReqNo = int.Parse(dv2.Rows[0]["ReqNum"].ToString()) + 1;
        //            }
        //            else
        //            {
        //                eReqNo = 1;
        //            }
        //        }
        //        else
        //        {
        //            eReqNo = 1;
        //        }
              
        //        varInsertPara[0] = eReqNo;
        //        varInsertPara[1] = (int)double.Parse(ExtObj.ReqNo);
        //        varInsertPara[2] = int.Parse(ExtObj.EmployeeNo);
        //        varInsertPara[3] = ExtObj.AccountingYear;
        //        varInsertPara[4] = (int)double.Parse(ExtObj.LeaveGrp);
        //        varInsertPara[5] = ExtObj.LvCode;
        //        varInsertPara[6] = int.Parse(ExtObj.RemainingDays);
        //        varInsertPara[7] = int.Parse(ExtObj.Duration);
        //        string[] ToDate = ExtObj.ToDate.Split('/');
        //        varInsertPara[8] = new DateTime(int.Parse(ToDate[2]), int.Parse(ToDate[1]), int.Parse(ToDate[0])).ToString("dd-MMM-yyyy");
        //        varInsertPara[9] =  "E";
        //        varInsertPara[10] = ExtObj.Reason;
        //        varInsertPara[11] = ExtObj.UserId;
        //        varInsertPara[12] = ExtObj.CompId;              

        //        IntResult = DataCreator.ExecuteProcedure("INSERT_H_EMP_LV_Extension", varInsertPara);

        //        if (IntResult == 0)
        //        {
        //            DataTable dv3 = LeaveExtObj.LeaveExtAutoNum(ExtObj.CompId, eReqNo, ExtObj.EmployeeNo);

        //            if (dv3.Rows.Count != 0)
        //            {
        //                intRqstID = int.Parse(dv3.Rows[0]["RQST_ID"].ToString());

        //                varEnginePara[0] = ExtObj.CompId;
        //                varEnginePara[1] = "LVR";
        //                varEnginePara[2] = intRqstID;
        //                varEnginePara[3] = ExtObj.EmployeeNo;
        //                varEnginePara[4] = 1;

        //                IntResult = DataCreator.ExecuteProcedure("REQUEST_PROCESS", varEnginePara);

        //                if (IntResult != 0)
        //                {
        //                    //'Delete The Application Record IF Request Processing Engine fails.
        //                    //'This will Make The Request Not Hanging Thereby Preventing The Employee
        //                    //'From Making Future Applications

        //                    StrQuery = "DELETE H_EMP_LV_RQST WHERE COMP_AID='" + ExtObj.CompId + "' AND RQST_ID=" + intRqstID;
        //                    DataCreator.ExecuteSQL(StrQuery);
        //                }
        //                else
        //                {
        //                    //    StrQuery = "UPDATE SS_APPROVAL_EMPLOYEES SET APPR_EMP_AID = '" + varInsertPara[16] + "' WHERE RQST_ID = " + intRqstID;
        //                    //    IntResultRow = DataCreator.ExecuteSQL(StrQuery);
        //                    object[] updateSsApprEmp = new object[4];
        //                    updateSsApprEmp[0] = ExtObj.CompId;
        //                    updateSsApprEmp[1] = intRqstID;
        //                    updateSsApprEmp[2] = ExtObj.RelOfficerId;
        //                    updateSsApprEmp[3] = ExtObj.UserId;

        //                    IntResultRow = DataCreator.ExecuteProcedure("UPDATE_SS_REQ_APPROVAL_RLF", updateSsApprEmp);

        //                    DataTable dv4 = LeaveExtObj.LeaveExtApprList(ExtObj.CompId, intRqstID);

        //                    if (dv4.Rows.Count > 0)
        //                    {

        //                        for (int ii = 0; ii < dv4.Rows.Count; ii++)
        //                        {                                   
        //                            intApprEmpID = DBNull.Value.Equals(dv4.Rows[ii]["appr_emp_aid"]) ? 0 : int.Parse(dv4.Rows[ii]["appr_emp_aid"].ToString());//CInt(IIf(IsDBNull(dv4.Item(i).Item("appr_emp_aid")), 0, dv4.Item(i).Item("appr_emp_aid")));
        //                            strApprovrEmail = DBNull.Value.Equals(dv4.Rows[ii]["apprvr_email"]) ? "" : dv4.Rows[ii]["apprvr_email"].ToString(); //IIf(IsDBNull(dv4.Item(i).Item("apprvr_email")), "", dv4.Item(i).Item("apprvr_email"));
        //                            strRqstrEmail = DBNull.Value.Equals(dv4.Rows[ii]["rqstr_email"]) ? "NEPTUNEHR" : dv4.Rows[ii]["rqstr_email"].ToString().Trim();// Trim(IIf(IsDBNull(dv4.Item(i).Item("rqstr_email")), "NEPTUNEHR", dv4.Item(i).Item("rqstr_email")));
        //                            strRqstEmp = DBNull.Value.Equals(dv4.Rows[ii]["requester"]) ? "" : dv4.Rows[ii]["requester"].ToString().Trim();// Trim(IIf(IsDBNull(dv4.Item(i).Item("requester")), "", dv4.Item(i).Item("requester")));

        //                            strSubject = "Leave";
                                  
        //                            string messageBody = strRqstEmp + "'s " + strRqstEmp + "'s " + strSubject + " request awaits your attention, kindly visit the Self Service Leave approval page to approve it";
                                    
        //                            if (strApprovrEmail != "")
        //                            {
        //                                MailRoutines(strApprovrEmail, " + strSubject + ", messageBody, ExtObj.CompId);

        //                                //'Sends Information to WorkFlow Viewer
        //                                intRes = SendAlerts(ExtObj.CompId, ExtObj.UserId, intApprEmpID, strRqstEmp + "'s " + strSubject + " request awaits your attention, kindly visit the Self Service Leave approval page to approve it", "Approvals", 12, intRqstID);

        //                            }
                                   
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //                IntResult = 1;      //error condition
        //        }

        //        string error;
        //        if (IntResult == 0) error = "";
        //        else error = "Error Saving Records";

        //        var response = new { Error = error };
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Log();
        //        throw;
        //    }

        //}

        [NonAction]
        public void MailRoutines(string recieverAddr, string subject, string body, string compCode)
        {
            new CommonMethodsClass().MailRoutines(recieverAddr, subject, body, compCode);
        }

        [NonAction]
        public int SendAlerts(string strComp, string strSender, int? intRecipientId, string strMessage, string apprReqType, int formID, int requestId)
        {
            return new CommonMethodsClass().SendAlerts(strComp, strSender, intRecipientId, strMessage, apprReqType, formID, requestId);
        }


        public class LeaveExtClass
        {
            public string ReqNo { get; set; }
            public string EmployeeNo { get; set; }
            public string AccountingYear { get; set; }
            public string LvCode { get; set; }
            public string RemainingDays { get; set; }
            public string Duration { get; set; }
            public string ToDate { get; set; }
            public string Reason { get; set; }
            public string LeaveGrp { get; set; }
            public string UserId { get; set; }
            public string CompId { get; set; }
            public int RelOfficerId { get; set; }
            public int PrevReqId { get; set; }
        }
    }


}
