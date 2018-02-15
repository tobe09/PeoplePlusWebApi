using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;

namespace PeoplePlusWebApi.Controllers
{
    [RoutePrefix("api/Training")]
    public class TrainingController : ApiController
    {
        private DataProvider.TrainingRequest _trnReqObj = new DataProvider.TrainingRequest();
        public DataProvider.TrainingRequest TrnReqObj
        {
            get { return _trnReqObj; }
        }

        [HttpGet]
        [Route("getTrainings")]
        public HttpResponseMessage getTrainings(string compId)
        {
            try
            {
                DataTable dt = TrnReqObj.ListOfTrainings(compId);
                if (dt == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent);
                }

                var output = new { Training = dt };

                return Request.CreateResponse(HttpStatusCode.OK, output);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }


        [Route("getNominees")]
        [HttpGet]
        public HttpResponseMessage getNominees(string TrainingCode, int empNo, string compId)
        {
            try
            {
                TrainingCode = TrainingCode.ToUpper();
                DataTable dt = TrnReqObj.SubordinateEmpBaseOnTraining(empNo, TrainingCode, compId);
                if (dt == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
            
                //string output = JsonConvert.SerializeObject(dt, Formatting.Indented);
                var output = new { Nominees = dt };
                return Request.CreateResponse(HttpStatusCode.OK, output);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }


        [Route("SaveNominee")]
        [HttpPost]
        public HttpResponseMessage SaveNominee([FromBody] TrainingParam item)
        {
            try
            {
                if (item == null || !ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                //bool itemExists = _toDoRepository.DoesItemExist(item.ID);
                //if (itemExists)
                //{
                //    return StatusCode(HttpStatusCode .Conflict );
                //}

                object[] varInsert = new object[8];

                varInsert[0] = item.CompanyCode;
                varInsert[1] = item.TrainingCode ;
                varInsert[2] = item.TrainingSerialNo;
                varInsert[3] = item.NomineeEmployeeNo;
                varInsert[4] = item.ReqEmployeeNo;
                varInsert[5] = item.Reason;
                varInsert[6] = item.Username;
                varInsert[7] = 0;

                var intResult = DataCreator.ExecuteProcedure("INSERT_H_TRNG_NMNTN", varInsert);


                if(intResult == 0)
                {

                    DataTable dsReqst = TrnReqObj.TrainingReqId(item.CompanyCode, item.TrainingCode, item.NomineeEmployeeNo, item.TrainingSerialNo);

                    //'StrQuery = "SELECT RQST_ID FROM H_TRNG_NMNTN WHERE  COMP_AID = '" & Session("Compid") & "' And HTNM_HEMP_EMPLYE_NMBR = " & txtEmpNo.Text & _
                    //'        " AND HTNM_TRNG_PRGRM_CODE ='" & lblTrnCode.Text & "' AND    HTNM_TRNG_SRL_NMBR = " & txtTranSN.Text

                    int intGlobReqestID = -1;
                    if (dsReqst.Rows.Count > 0)
                    {
                        intGlobReqestID = DBNull.Value.Equals(dsReqst.Rows[0]["RQST_ID"]) ? -1 : Convert.ToInt32(dsReqst.Rows[0]["RQST_ID"]);
                    
                    }
                   
                    object[] varEnginePara = new object[5];

                    varEnginePara[0] = item.CompanyCode;
                    varEnginePara[1] = "TRN";
                    varEnginePara[2] = intGlobReqestID;
                    varEnginePara[3] = item.ReqEmployeeNo;
                    varEnginePara[4] = 1;

                    var result = DataCreator.ExecuteProcedure("REQUEST_PROCESS", varEnginePara);

                    if (result != 0)
                    {
                        var resultRows = TrnReqObj.DeleteFromSSApprovalTable(item.CompanyCode, intGlobReqestID);
                                                
                    }
                    else
                    {
                        DataTable  dset =TrnReqObj.SelectSSGrievGLBMail(item.CompanyCode, intGlobReqestID);

                        if (dset.Rows.Count > 0) 
                        {
                   
                            for (int i = 0; i<=(dset.Rows.Count-1);i++)
                            {
                                int intApprEmpID = DBNull.Value.Equals(dset.Rows[i]["appr_emp_aid"]) ? 0 : Convert.ToInt32 (dset.Rows[i]["appr_emp_aid"]);
                                string strApprovrEmail = DBNull.Value.Equals(dset.Rows[i]["apprvr_email"]) ? "" : Convert.ToString (dset.Rows[i]["apprvr_email"]);
                                string strRqstrEmail = DBNull.Value.Equals(dset.Rows[i]["rqstr_email"]) ? "NILEBANKHR" : Convert.ToString(dset.Rows[i]["rqstr_email"]);
                                var strRqstEmp = DBNull.Value.Equals(dset.Rows[i]["requester"]) ? "" : dset.Rows[i]["requester"];

                                string strSubject = "";
                                strSubject = "Training";

                                string messageBody = strRqstEmp + "'s " + strSubject + " request awaits your attention. Kindly visit the Self Service Training approval page to handle request";
                                    
                                if (strApprovrEmail != "") 
                                {                                        
                                   new CommonMethodsClass().MailRoutines(strApprovrEmail, strSubject, messageBody, item.CompanyCode);
                                }

                                new NotificationController().SendNotification(item.CompanyCode, intGlobReqestID, intApprEmpID, item.Username,
                                    strSubject, strRqstEmp + "'s " + strSubject + " request awaits your attention. Kindly visit the WorkListViewer to approve it");

                                new CommonMethodsClass().SendAlerts(item.CompanyCode, item.Username, intApprEmpID, messageBody, "Approvals", 27, intGlobReqestID);
                            }
                        }
                    }

                }

                if (intResult == 0)
                {
                    var output = new { message = "Saved Successfully", status = intResult };
                    return Request.CreateResponse(HttpStatusCode.Created , output);
                }
                else {
                    var output = new { message = "Error Saving Nominee", status = intResult };
                    return Request.CreateResponse(output);
                }

            }
            catch (Exception ex)
            {
                ex.Log();
                return Request.CreateResponse(HttpStatusCode.BadRequest);;
            }
        }



        //[Route("ApproveNominee")]
        //[HttpPost]
        //public HttpResponseMessage ApproveNominee([FromBody] ApprovTrainingParam item)
        //{
        //    try
        //    {
        //        if (item == null || !ModelState.IsValid)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest);
        //        }
        //        //bool itemExists = _toDoRepository.DoesItemExist(item.ID);
        //        //if (itemExists)
        //        //{
        //        //    return StatusCode(HttpStatusCode .Conflict );
        //        //}

        //        object[] varModifyPara = new object[11];

        //        varModifyPara[0] = item.CompanyCode;
        //        varModifyPara[1] = item.ProgramCode;
        //        varModifyPara[2] = item.TrainingSerialNo;
        //        varModifyPara[3] = item.NomineeEmployeeNo;
        //        varModifyPara[4] = item.IsApproved.Equals (true) ? "A" : "D";
        //        varModifyPara[5] = item.ModifiedDate;
        //        varModifyPara[6] = "TRN";
        //        varModifyPara[7] = item.Reason;
        //        varModifyPara[8] = item.ApprEmployeeNo;
        //        varModifyPara[9] = item.Username;
        //        varModifyPara[10] = item.RequestID;

        //        var intResult = (new EmployeeDetails()).ExecuteProcedure("UPDATE_SS_TRANAPPROVAL", varModifyPara);
                
        //        if (intResult == 0)
        //            {

        //                DataView dset = (new EmployeeDetails()).SelectViewTrngCurrStg(item.CompanyCode, item.RequestID);

        //                //'StrQuery = "SELECT RQST_ID FROM H_TRNG_NMNTN WHERE  COMP_AID = '" & Session("Compid") & "' And HTNM_HEMP_EMPLYE_NMBR = " & txtEmpNo.Text & _
        //                //'        " AND HTNM_TRNG_PRGRM_CODE ='" & lblTrnCode.Text & "' AND    HTNM_TRNG_SRL_NMBR = " & txtTranSN.Text

        //                if (dset.Table .Rows.Count > 0)
        //                {
        //                    char strStatus = DBNull.Value.Equals(dset.Table .Rows [0]["HTNM_STS"]) ? '\0' : Convert.ToChar(dset.Table .Rows[0]["HTNM_STS"]);
        //                    int intCurrentStage = DBNull.Value.Equals(dset.Table.Rows[0]["RQST_CUR_STAGE"]) ? -1 : Convert.ToInt32(dset.Table.Rows[0]["RQST_CUR_STAGE"]);
                            
        //                if (char.ToUpper(strStatus) == 'U' || char.ToUpper(strStatus) == 'D')
        //                {
        //                    var resultRows = (new EmployeeDetails()).DeleteFromSSApprovalTable(item.CompanyCode, item.RequestID);
        //                }
        //                else
        //                {
        //                    object[] varEnginePara = new object[5];

        //                    varEnginePara[0] = item.CompanyCode;
        //                    varEnginePara[1] = "TRN";
        //                    varEnginePara[2] = item.RequestID;
        //                    varEnginePara[3] = item.ApprEmployeeNo;
        //                    varEnginePara[4] = intCurrentStage;

        //                    var result = (new EmployeeDetails()).ExecuteProcedure("REQUEST_PROCESS", varEnginePara);


        //                    if (result == 0)
        //                    {
        //                        dset = (new EmployeeDetails()).SelectSSGrievGLBMail(item.CompanyCode, item.RequestID);

        //                        if (dset.Count > 0)
        //                        {

        //                            for (int i = 0; i <= (dset.Count - 1); i++)
        //                            {
        //                                int intApprEmpID = DBNull.Value.Equals(dset.Table.Rows[i]["appr_emp_aid"]) ? 0 : Convert.ToInt32(dset.Table.Rows[i]["appr_emp_aid"]);
        //                                string strApprovrEmail = DBNull.Value.Equals(dset.Table.Rows[i]["apprvr_email"]) ? "" : Convert.ToString(dset.Table.Rows[i]["apprvr_email"]);
        //                                string strRqstrEmail = DBNull.Value.Equals(dset.Table.Rows[i]["rqstr_email"]) ? "NILEBANKHR" : Convert.ToString(dset.Table.Rows[i]["rqstr_email"]);
        //                                var strRqstEmp = DBNull.Value.Equals(dset.Table.Rows[i]["requester"]) ? "" : dset.Table.Rows[i]["requester"];

        //                                string strSubject = "";
        //                                strSubject = "Training";

        //                                string messageBody = strRqstEmp + "'s " + strSubject + " request awaits your attention, kindly visit the Self Service Training approval page to handle request";

        //                                if (strApprovrEmail != "")
        //                                {

        //                                    new CommonMethodsClass().MailRoutines(strApprovrEmail, strSubject, messageBody, item.CompanyCode);
        //                                }

        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        if (intResult == 0)
        //        {
        //            var output = new { message = "Saved Successfully", status = intResult };
        //            return Request.CreateResponse(HttpStatusCode.NoContent, output);
        //        }
        //        else
        //        {
        //            var output = new { message = "Error Saving Record", status = intResult };
        //            return Request.CreateResponse(HttpStatusCode.Created, output);
        //        }

        //    }
        //    catch (Exception)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.BadRequest); ;
        //    }
        //}

        //[NonAction]
        //public int SendAlerts(string strComp, string strSender, int? intRecipientId, string strMessage, string ApprReqType, int FormID)
        //{
        //    object[] insertPara = new object[10];
        //    object[] insertMapPara = new object[2];
        //    string strAc = "";

        //    switch (ApprReqType)
        //    {
        //        case "Approvals": strAc = "APR";
        //            break;
        //        case "Birthdays": strAc = "BIR";
        //            break;
        //        case "Holidays": strAc = "HOL";
        //            break;
        //        default: strAc = "";
        //            break;
        //    }

        //    insertPara[0] = strComp;
        //    insertPara[1] = strAc;
        //    insertPara[2] = strMessage;
        //    insertPara[3] = intRecipientId == 0 ? null : intRecipientId;
        //    insertPara[4] = strAc == "BIR" || strAc == "HOL" ? "A" : "R";
        //    insertPara[5] = strSender;
        //    insertPara[6] = 1;
        //    insertPara[7] = "";
        //    insertPara[8] = 1;
        //    insertPara[9] = DateTime.Now.ToString("dd-MMM-yyyy");

        //    int status = (new EmployeeDetails()).ExecuteProcedure("INSERT_GM_ALERTS", insertPara);

        //    if (status == 0)
        //    {
        //        insertMapPara[0] = FormID;
        //        insertMapPara[1] = strSender;
        //        status = (new EmployeeDetails()).ExecuteProcedure("INSERT_SS_ALERTID_MAP", insertMapPara);
        //    }

        //    return status;
        //}


    }

    public class TrainingParam
    {
        private string _companyCode;
        private string _trainingCode;
        private string _reason;
        private string _username;

        public string CompanyCode { get { return _companyCode; } set { _companyCode = value.ToUpper(); } }
        public string TrainingCode { get { return _trainingCode; } set { _trainingCode = value.ToUpper(); } }
        public int TrainingSerialNo { get; set; }
        public int NomineeEmployeeNo { get; set; }
        public int ReqEmployeeNo { get; set; }
        public string Reason { get { return _reason; } set { _reason = value.ToUpper(); } }
        public string Username { get { return _username; } set { _username = value.ToUpper(); } }
    }

    public class ApprovTrainingParam
    {
        private string _companyCode;
        private string _programCode;
        private string _reason;
        private string _username;

        public string CompanyCode { get { return _companyCode; } set { _companyCode = value.ToUpper(); } }
        public string ProgramCode { get { return _programCode; } set { _programCode = value.ToUpper(); } }
        public int TrainingSerialNo { get; set; }
        public int NomineeEmployeeNo { get; set; }
        public bool IsApproved { get; set; }
        public string ModifiedDate { get; set; }
        public string Reason { get { return _reason; } set { _reason = value.ToUpper(); } }
        public int ApprEmployeeNo { get; set; }
        public string Username { get { return _username; } set { _username = value.ToUpper(); } }
        public int RequestID { get; set; }
    }

    

}
