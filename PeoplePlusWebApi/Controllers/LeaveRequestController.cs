using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using System.Net.Mail;



namespace PeoplePlusWebApi.Controllers
{
    public class LeaveRequestController : ApiController
    {
        private DataProvider.LeaveRequest _leaveReqObj = new DataProvider.LeaveRequest();
        public DataProvider.LeaveRequest LeaveReqObj { get { return _leaveReqObj; } }
       
        public object GetLeaveType(string compId,int EmployeeNo)
        {
            DataTable LeaveType = LeaveReqObj.GetLeaveType(compId,EmployeeNo);
            var response = new { Values = LeaveType };
            return response;            
        }

        public object GetReliefOfficer(string compId, string LocationCode, string Department, string EmployeeNo)
        {
            DataTable ReliefOfficer = LeaveReqObj.GetReliefOfficer(compId, LocationCode, Department, EmployeeNo);
            var response = new { Values = ReliefOfficer };
            return response;
        }

        public string GetLeaveDays(string compId, int EmployeeNo, string Leavecode, string AccountingYear, int StartDate, int EndDate)
        {
            try
            {
                string RmnDays = "";
                DataTable LVGrp = LeaveReqObj.ChkLeaveGroup(compId, EmployeeNo);
                int lblLvGrp = int.Parse(LVGrp.Rows[0]["LvGrpCode"].ToString());

                DataTable dv = LeaveReqObj.chkLeaveRemain(lblLvGrp, compId, Leavecode, AccountingYear, EmployeeNo);
                if (dv.Rows.Count == 1)
                {
                    var response = new { Values = dv };
                    RmnDays = dv.Rows[0]["LvRmn"].ToString();
                    return RmnDays;
                }
                else
                {
                    DataTable dv1 = LeaveReqObj.chkLeaveRemains(lblLvGrp, Leavecode, compId);
                    if (dv1.Rows.Count > 0)
                    {
                        var response = new { Values = dv1 };
                        RmnDays = dv1.Rows[0]["LvRmn"].ToString();
                        return RmnDays;
                    }
                    if (int.Parse(dv1.Rows[0]["Depends"].ToString()) > 0)
                    {
                        DataTable dv2 = LeaveReqObj.chkLeaveRemained(compId, Leavecode);
                        if (dv2.Rows.Count > 0)
                        {
                            if (int.Parse(dv2.Rows[0]["LvRmn"].ToString()) == 1)
                            {
                                DataTable lblJoingDt = LeaveReqObj.EmpJoingDate(compId, EmployeeNo);
                                string lblJnDate = lblJoingDt.Rows[0]["JoingDate"].ToString();

                                DataTable AcctYear = LeaveReqObj.GetAccountYear(compId);
                                int g_datDateFrom = int.Parse(AcctYear.Rows[0]["from_date"].ToString());
                                int g_datDateTo = int.Parse(AcctYear.Rows[0]["to_date"].ToString());

                                if (int.Parse(lblJnDate) > g_datDateFrom && int.Parse(lblJnDate) < g_datDateTo)
                                {
                                    RmnDays = "" + (int.Parse(lblJnDate) + 1) / ((EndDate - (StartDate + 1) * int.Parse(Leavecode)));
                                    return RmnDays;
                                }
                                else
                                {
                                    return RmnDays;
                                }
                            }
                            else
                            {
                                return RmnDays;
                            }
                        }
                    }
                }
                return RmnDays;
            }
            catch (Exception ex)
            {
                ex.Log();
                throw;
            }
        }

        public string GetActiveLeave(string compId, string LvCode,string EmployeeNo)
        {
            string MessageBody = "";

            try
            {               
                DataTable dv = LeaveReqObj.SLeaveReqCheck(compId, EmployeeNo);
                if (dv.Rows.Count <= 0)
                {
                    DataTable dv1 = LeaveReqObj.SLeaveReqCheck1(compId, EmployeeNo, LvCode);

                    if (dv1.Rows.Count > 0)
                    {
                         MessageBody = "Sorry You Already Have A Leave Running or Approved. Do you want to Extend it ?";
                        return MessageBody;
                    }
                }

                return MessageBody;
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        public int GetWorkDays(string strFromDate, string strToDate, string compId)
        {
            return new CommonMethodsClass().GetWorkDays(strFromDate, strToDate, compId);
        }

        public EventHandler<int> NewRequestIdEvent;
        public object PostRequest([FromBody] LeaveReqClass requestObj, bool isExtension=false)
        {
            string startdate = requestObj.StartdateStr;
            DateTime? startDt = new CommonMethodsClass().ConvertStringToDate(startdate);
            if (DateTime.Now >= startDt || startDt == null)
            {
                return new { Error = "Start date must be greater than today's date" };
            }

            object[] varAnnualLv = new object[6];
            object[] varCasualLv = new object[6];
            object[] varInsertPara = new object[18];
            object[] varInsertRemainData = new object[7];
            object[] varModifyPara = new object[18];
            object[] varEnginePara = new object[5];


            int intRqstID;
            int IntResult;
            int IntResultRow;
            string strApprovrEmail, strRqstrEmail, strRqstEmp, strSubject;
            int intCheckLvDepends = 0;
            string strAnnlLvCode = "";
            string strLvCode;
            int intLvRec;
            int intRes;
            int intApprEmpID;
            string StrQuery;

            try
            {

                int LeaveGrp;
                DataTable dvx = LeaveReqObj.ChkLeaveGroup(requestObj.CompId, int.Parse(requestObj.EmployeeNo));
                if (dvx.Rows.Count > 0)
                    LeaveGrp = int.Parse(dvx.Rows[0]["LvGrpCode"].ToString()); 
                else
                    LeaveGrp = 0; 

                int nReqNo;

                DataTable dv = LeaveReqObj.SLeaveReqTypeDtl(requestObj.CompId, requestObj.LvCode);

                if (dv.Rows.Count > 0)
                {
                    for (int II = 0; II < dv.Rows.Count; II++)
                    {
                        if (dv.Rows[II]["ANL"] != DBNull.Value)
                        {
                            intCheckLvDepends = int.Parse(dv.Rows[II]["ANL"].ToString());
                        }
                        if (dv.Rows[II]["h_leave_code"] != DBNull.Value)
                        {
                            strAnnlLvCode = dv.Rows[II]["h_leave_code"].ToString();
                        }
                        intCheckLvDepends = !DBNull.Value.Equals(dv.Rows[II]["ANL"]) ? 1 : 1;
                    }
                }
                if (intCheckLvDepends == 1)
                    strLvCode = strAnnlLvCode;
                else
                    strLvCode = requestObj.LvCode;

                DataTable dv1 = LeaveReqObj.SLeaveReqAutoNum(requestObj.CompId, requestObj.AccountingYear, requestObj.EmployeeNo, requestObj.LvCode);

                if (dv1.Rows.Count == 0)
                    intLvRec = 1;
                else
                    intLvRec = 0;
                
                    //Select Maximun Request Number And Increment For this Request
                    DataTable dv2 = LeaveReqObj.SLeaveReqNum(requestObj.CompId);
                    if (dv2.Rows.Count > 0)
                    {
                        if (dv2.Rows[0]["ReqNum"] != DBNull.Value)
                            nReqNo = int.Parse(dv2.Rows[0]["ReqNum"].ToString()) + 1;
                        else
                            nReqNo = 1;
                    }
                    else
                    {
                        nReqNo = 1;
                    }

                varInsertPara[0] = nReqNo;
                varInsertPara[1] = requestObj.EmployeeNo;   // Session("HEMP_EMPLYE_NMBR");
                varInsertPara[2] = requestObj.AccountingYear;  //Session("acc_year");
                varInsertPara[3] = LeaveGrp;                //Leave Grp
                varInsertPara[4] = requestObj.LvCode;   //lblLeave.Text;
                varInsertPara[5] = requestObj.RemainingDays;   // CInt(txtRDays.Text) ;
                varInsertPara[6] = requestObj.Duration; // CInt(txtDuration1.Text);
                string[] strtDate = requestObj.StartdateStr.Split('/');
                varInsertPara[7] = new DateTime(int.Parse(strtDate[2]), int.Parse(strtDate[1]), int.Parse(strtDate[0])).ToString("dd-MMM-yyyy");
                //varInsertPara[7] = DateTime.Parse(requestObj.StartdateStr).ToString("dd-MMM-yyyy");    // DateTime.Parse(Trim(txtStartDt.Text)).ToString("dd-MMM-yyyy");
                string[] EndDate = requestObj.EnddateStr.Split('/');
                varInsertPara[8] = new DateTime(int.Parse(EndDate[2]), int.Parse(EndDate[1]), int.Parse(EndDate[0])).ToString("dd-MMM-yyyy");
                // varInsertPara[8] = DateTime.Parse(requestObj.EnddateStr).ToString("dd-MMM-yyyy");  // DateTime.Parse(Trim(txtEndDt.Text)).ToString("dd-MMM-yyyy");
                varInsertPara[9] = requestObj.reason;   //     UCase(txtReason.Text);
                varInsertPara[10] = requestObj.Allowance;// IIf(chkAllowance.Checked = True, "Y", "N");
                varInsertPara[11] = "";
                varInsertPara[12] = "";
                varInsertPara[13] = "R";
                varInsertPara[14] = requestObj.UserId;//Session("UName");
                varInsertPara[15] = requestObj.CompId;// Session("Company_CODE");
                varInsertPara[16] = requestObj.ReliefOfficerID;
                varInsertPara[17] = requestObj.ReliefOfficer;

                IntResult = DataCreator.ExecuteProcedure("INSERT_SS_H_EMP_LV_RQST", varInsertPara);

                varCasualLv[0] = varInsertPara[1];
                varCasualLv[1] = varInsertPara[3];
                varCasualLv[2] = varInsertPara[4];
                varCasualLv[3] = varInsertPara[5];

                //To be looked at later(What is this used for)
                //IntResult = DataCreator.ExecuteProcedure("UPDATE_CASUAL_LV", varCasualLv);                  

                if (IntResult == 0)
                {
                    if ((IntResult == 0 && intCheckLvDepends == 0 && intLvRec == 1) || (IntResult == 0 && intCheckLvDepends == 1 && intLvRec == 1))
                    {
                        varInsertRemainData[0] = requestObj.AccountingYear; // Session("acc_year");
                        varInsertRemainData[1] = requestObj.EmployeeNo; //Session("HEMP_EMPLYE_NMBR");
                        varInsertRemainData[2] = LeaveGrp; // Val(lblLvGrp.Text);
                        varInsertRemainData[3] = requestObj.LvCode;  //lblLvCode.Text ;    //' Trim(cboH_LV_DESC.Value)
                        varInsertRemainData[4] = requestObj.RemainingDays; // IIf((txtRDays.Text = ""), "", Val(txtRDays.Text)) ; // 'Val(lblRmNoOfDays)
                        varInsertRemainData[5] = requestObj.UserId;  //Trim(Session("Username"));
                        varInsertRemainData[6] = requestObj.CompId;  //Session("Company_CODE");

                        IntResult = DataCreator.ExecuteProcedure("INSERT_H_EMP_LV_RMN_DAYS", varInsertRemainData);
                    }

                    else if (intCheckLvDepends == 0 && strLvCode != requestObj.LvCode)
                    {
                        varInsertRemainData[0] = requestObj.AccountingYear; //Session("acc_year");
                        varInsertRemainData[1] = requestObj.EmployeeNo; // Session("HEMP_EMPLYE_NMBR");
                        varInsertRemainData[2] = LeaveGrp; // Val(lblLvGrp.Text);
                        varInsertRemainData[3] = requestObj.LvCode; //lblLvCode.Text;
                        varInsertRemainData[4] = requestObj.RemainingDays; //IIf((txtRDays.Text = ""), "", Val(txtRDays.Text));
                        varInsertRemainData[5] = requestObj.UserId; // Trim(Session("Username"));
                        varInsertRemainData[6] = requestObj.CompId; //Session("Company_CODE");

                        IntResult = DataCreator.ExecuteProcedure("INSERT_H_EMP_LV_RMN_DAYS", varInsertRemainData);
                    }

                    DataTable dv3 = LeaveReqObj.SLeaveReqNumPer(requestObj.CompId, nReqNo, requestObj.EmployeeNo);

                    //'StrQuery = "SELECT  RQST_ID FROM H_EMP_LV_RQST WHERE COMP_AID ='" & Session("Company_CODE") & "' AND H_RQSN_NMBR=" & nReqNo & " AND H_EMPLYE_NMBR=" & Session("gsEmpNo")
                    //'dsReqst = GetDisConnectedDataset(StrQuery)

                    if (dv3.Rows.Count != 0)
                    {
                        intRqstID = int.Parse(dv3.Rows[0]["RQST_ID"].ToString());
                        NewRequestIdEvent?.Invoke(this, intRqstID);

                        varEnginePara[0] = requestObj.CompId; //Session("Company_CODE");
                        varEnginePara[1] = "LVR";
                        varEnginePara[2] = intRqstID;
                        varEnginePara[3] = requestObj.EmployeeNo; // Session("HEMP_EMPLYE_NMBR");
                        varEnginePara[4] = 0;

                        IntResult = DataCreator.ExecuteProcedure("REQUEST_PROCESS", varEnginePara);

                        if (IntResult != 0)
                        {
                            //'Delete The Application Record IF Request Processing Engine fails.
                            //'This will Make The Request Not Hanging Thereby Preventing The Employee
                            //'From Making Future Applications

                            StrQuery = "DELETE H_EMP_LV_RQST WHERE COMP_AID='" + requestObj.CompId + "' AND RQST_ID=" + intRqstID;
                            DataCreator.ExecuteSQL(StrQuery);
                        }
                        else
                        {
                            //    StrQuery = "UPDATE SS_APPROVAL_EMPLOYEES SET APPR_EMP_AID = '" + varInsertPara[16] + "' WHERE RQST_ID = " + intRqstID;
                            //    IntResultRow = DataCreator.ExecuteSQL(StrQuery);
                            object[] updateSsApprEmp = new object[4];
                            updateSsApprEmp[0] = requestObj.CompId;
                            updateSsApprEmp[1] = intRqstID;
                            updateSsApprEmp[2] = varInsertPara[16];
                            updateSsApprEmp[3] = requestObj.UserId;

                            IntResultRow = DataCreator.ExecuteProcedure("UPDATE_SS_REQ_APPROVAL_RLF", updateSsApprEmp);


                            DataTable dv4 = LeaveReqObj.LeaveApprovalList(intRqstID, requestObj.CompId);

                            if (dv4.Rows.Count > 0)
                            {

                                for (int ii = 0; ii < dv4.Rows.Count; ii++)
                                {
                                    intApprEmpID = DBNull.Value.Equals(dv4.Rows[ii]["appr_emp_aid"]) ? 0 : int.Parse(dv4.Rows[ii]["appr_emp_aid"].ToString());//CInt(IIf(IsDBNull(dv4.Item(i).Item("appr_emp_aid")), 0, dv4.Item(i).Item("appr_emp_aid")));
                                    strApprovrEmail = DBNull.Value.Equals(dv4.Rows[ii]["apprvr_email"]) ? "" : dv4.Rows[ii]["apprvr_email"].ToString(); //IIf(IsDBNull(dv4.Item(i).Item("apprvr_email")), "", dv4.Item(i).Item("apprvr_email"));
                                    strRqstrEmail = DBNull.Value.Equals(dv4.Rows[ii]["rqstr_email"]) ? "NEPTUNEHR" : dv4.Rows[ii]["rqstr_email"].ToString().Trim();// Trim(IIf(IsDBNull(dv4.Item(i).Item("rqstr_email")), "NEPTUNEHR", dv4.Item(i).Item("rqstr_email")));
                                    strRqstEmp = DBNull.Value.Equals(dv4.Rows[ii]["requester"]) ? "" : dv4.Rows[ii]["requester"].ToString().Trim();// Trim(IIf(IsDBNull(dv4.Item(i).Item("requester")), "", dv4.Item(i).Item("requester")));

                                    strSubject = "Leave";

                                    string reqType = "Request";
                                    if (isExtension) reqType = "Extension";
                                    string messageBody = strRqstEmp + "'s " + strSubject + " Your Leave "+reqType+" Has Been Submitted For '" + requestObj.Duration + 
                                        "'Day(s). To Commence On '" + requestObj.StartdateStr + "'. Your Resumption Date Is On '" + requestObj.EnddateStr + "' ";
                                    
                                    if (!string.IsNullOrEmpty(strApprovrEmail))
                                    {
                                        //'Call MailRoutines(strRqstrEmail, strApprovrEmail, strSubject, strRqstEmp & "'s " & strSubject & " your loan or leave request has been approved", Session("Company_CODE"))
                                        MailRoutines(strApprovrEmail, strSubject, messageBody, requestObj.CompId);
                                    }

                                    //Sends Information to WorkFlow Viewer
                                    //intRes = SendAlerts(Session("Company_CODE"), Session("UName"), intApprEmpID, strRqstEmp & "'s " & strSubject & " Your Leave Request Has Been Submitted For '" & txtDuration1.Text & "'Day(s). To Commence On '" & txtStartDt.Text & "' And Ends On '" & Val(txtEndDt.Text) - 1 & "'. Your Resumption Date Is On '" & txtEndDt.Text & "'", 12)
                                    intRes = SendAlerts(requestObj.CompId, requestObj.UserId, intApprEmpID, strRqstEmp + "'s " + strSubject + " " + reqType +
                                        " awaits your attention. Kindly visit the Self Service Leave approval page to approve it", "Approvals", 12, intRqstID);
                                    //intRes = DismissAlert(requestObj.CompId,0,requestObj.UserId);

                                    new NotificationController().SendNotification(requestObj.CompId, intRqstID, requestObj.ReliefOfficerID, requestObj.UserId, strSubject,
                                        strRqstEmp + "'s " + strSubject + " " + reqType + "  awaits your attention. Kindly visit the WorkList Viewer to approve it");

                                    ////'To send Alert To Alert Table
                                    //intRes = SendAlerts2(requestObj.CompId, requestObj.UserId, intApprEmpID, strRqstEmp + "'s " + strSubject + " " + reqType +
                                    //    " awaits your attention, kindly visit the Self Service Leave approval page to approve it", "Approvals", 12, intRqstID);

                                    intRes = SendAlerts2(requestObj.CompId, requestObj.UserId, int.Parse(requestObj.EmployeeNo), strRqstEmp + ": " + "Your Leave " +
                                        reqType + " has been Submitted", "Approvals", 12, intRqstID);

                                }
                            }
                        }
                    }
                    else
                        IntResult = 1;      //error condition
                }

                string error;
                if (IntResult == 0) error = "";
                else error = "Error Saving Records";

                var response = new { Error = error };
                return response;
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }
                
        [NonAction]
        public void MailRoutines(string recieverAddr, string subject, string body, string compCode)
        {
            new CommonMethodsClass().MailRoutines(recieverAddr, subject, body, compCode);
        }

        [NonAction]
        public int SendAlerts(string strComp, string strSender, int? intRecipientId, string strMessage, string ApprReqType, int FormID, int requestId)
        {
            return new CommonMethodsClass().SendAlerts(strComp, strSender, intRecipientId, strMessage, ApprReqType, FormID, requestId);
        }

        public int SendAlerts2(string strComp, string strSender, int? intRecipientId, string strMessage, string ApprReqType, int FormID, int requestId)
        {
            return new CommonMethodsClass().SendAlerts(strComp, strSender, intRecipientId, strMessage, ApprReqType, FormID, requestId, isDefault: false);
        }

         //public int DismissAlert(string Comp_Id,int AlertId, string UserName) 
         //{         
          
         //   object[] UpdatePara = new object[5];
         //   UpdatePara[0] = Comp_Id; // Session("Company_CODE") 'SP_COMP_AID
         //   UpdatePara[1] = AlertId;    //Session("AlertID") 'SP_ALERT_ID
         //   UpdatePara[2] = 0;  // 'SP_ALERT_TIMES
         //   UpdatePara[3] = "D";    // 'SP_ALERT_STATUS
         //   UpdatePara[4] =  UserName;   //Session("Username") 'SP_MDFD_BY

         //   int status = DataCreator.ExecuteProcedure("UPDATE_GM_ALERTS", UpdatePara);
         //   return status;
         //}
      
       
        public class LeaveReqClass
        {
            public string EmployeeNo { get; set; }
            public string AccountingYear { get; set; }
            public string LvCode { get; set; }
            public string RemainingDays { get; set; }
            public string Duration { get; set; }
            public string StartdateStr { get; set; }
            public string EnddateStr { get; set; }
            public string reason { get; set; }
            public string Allowance { get; set; }
            public string UserId { get; set; }
            public string CompId { get; set; }
            public int ReliefOfficerID { get; set; }
            public string ReliefOfficer { get; set; }
        }

    }
}
