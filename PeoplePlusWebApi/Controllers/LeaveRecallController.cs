using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;

namespace PeoplePlusWebApi.Controllers
{
    public class LeaveRecallController : ApiController
    {

        public DataProvider.LeaveRecall LeaveRecObj { get { return new DataProvider.LeaveRecall(); } }

        public object GetEmployeeLv(string compId, int EmployeeNo)
        {
            DataTable LeaveType = LeaveRecObj.GetEmployee(compId, EmployeeNo);
            var response = new { Values = LeaveType };
            return response;
        }

        public object GetLeaveInfo(string compId, int RqstNo,int EmpNo)
        {
            DataTable LeaveTypeInfo = LeaveRecObj.GetEmployeeInfo(compId, RqstNo);
            var response = new { Values = LeaveTypeInfo };
            return response;
        }

        public int GetWorkDays(string strFromDate, string strToDate, string compId)
        {
            return new CommonMethodsClass().GetWorkDays(strFromDate, strToDate, compId);
        }

        public object PostRequest([FromBody] LeaveRecClass recallObj)
        {
            string startdate = recallObj.RecallDt;
            DateTime? startDt = new CommonMethodsClass().ConvertStringToDate(startdate);
            if (DateTime.Now >= startDt || startDt == null)
            {
                return new { Error = "Recall date must be greater than today's date" };
            }

            object[] varModifyMasterData = new object[8];
            varModifyMasterData[0] = recallObj.CompId;
            varModifyMasterData[1] = recallObj.GRqstID;
            varModifyMasterData[2] = recallObj.RecallEmp;
            varModifyMasterData[3] = recallObj.SpentDays;
            varModifyMasterData[4] = recallObj.AccountingYear;
            varModifyMasterData[5] = DateTime.Parse(recallObj.RecallDt).ToString("dd-MMM-yyyy");
            varModifyMasterData[6] = recallObj.UserId;
            varModifyMasterData[7] = DateTime.Now.ToString("dd-MMM-yyyy");

            int result = DataCreator.ExecuteProcedure("UPDATE_H_EMP_LV_RQST_R", varModifyMasterData);

            if (result == 0) {

                DataTable dt = LeaveRecObj.LvlRecallDays(recallObj.CompId, recallObj.RqstId, recallObj.RecallEmp, recallObj.AccountingYear, recallObj.LvGrp, recallObj.LvCode);

                int unspent = 0;
                if (dt.Rows.Count > 0)
                    unspent = int.Parse(dt.Rows[0]["DateTo"].ToString());

                object[] varModifyData = new object[8];
                varModifyData[0] = recallObj.CompId;
                varModifyData[1] = recallObj.LvGrp;
                varModifyData[2] = recallObj.RecallEmp;
                varModifyData[3] = int.Parse(recallObj.OldDuration) - int.Parse(recallObj.SpentDays);
                varModifyData[4] = recallObj.AccountingYear;
                varModifyData[5] = recallObj.LvCode;
                varModifyData[6] = recallObj.UserId;
                varModifyData[7] = DateTime.Now.ToString("dd-MMM-yyyy");

                result = DataCreator.ExecuteProcedure(" UPDATE_H_EMP_LV_RMN_DAYS_R", varModifyData);
            }

            string error;
            if (result == 0) error = "";
            else error = "Error Saving Records";

            var response = new { Error = error };
            return response;
        }


        //public void FormerPost(){/{

        //    string StrQuery, StrQuery0, StrQuery1;
        //    int IntResult = 0;
        //    int intUnspent = 0;
        //    int intCheckLvDepends = 0;
        //    string strAnnlLvCode = "";
        //    string strLvCode;
        //    object[] varInsertRemainData = new object[7];

        //    string[] EndDate = recallObj.RecallDt.Split('/');
        //    string RecDate = new DateTime(int.Parse(EndDate[2]), int.Parse(EndDate[1]), int.Parse(EndDate[0])).ToString("dd-MMM-yyyy");
        //    //       string RecDate= (string)DateTime.Parse(recallObj.RecallDt).ToString("dd-MMM-yyyy");

        //    try
        //    {

        //        StrQuery = " UPDATE H_EMP_LV_RQST  SET H_ACTUAL_SPNT_DAYS = " + recallObj.spentDays + " , P_DATE_TO = to_date('" + RecDate + "'), " +
        //                   " A_DATE_TO = To_date('" + RecDate + "'), LV_STS = 'C'  , LV_RST_MDFD_BY = '" + recallObj.UserId + "', LV_RST_MDFD_DATE = SYSDATE " +
        //                   " WHERE COMP_AID = '" + recallObj.CompId + "' And RQST_ID = '" + recallObj.GRqstID + "'  " +
        //                   " AND H_EMPLYE_NMBR = " + recallObj.RecallEmp + " AND H_ACC_YEAR = '" + recallObj.AccountingYear + "' ";
        //        IntResult = DataCreator.ExecuteSQL(StrQuery);

        //        if (IntResult == 0)
        //        {
        //            DataTable dv = LeaveRecObj.SLeaveReqTypeDtl(recallObj.CompId, recallObj.LvCode);

        //            if (dv.Rows.Count > 0)
        //            {
        //                for (int II = 0; II < dv.Rows.Count; II++)
        //                {
        //                    if (dv.Rows[II]["ANL"] != System.DBNull.Value)
        //                    {
        //                        intCheckLvDepends = int.Parse(dv.Rows[II]["ANL"].ToString());
        //                    }
        //                    if (dv.Rows[II]["h_leave_code"] != System.DBNull.Value)
        //                    {
        //                        strAnnlLvCode = dv.Rows[II]["h_leave_code"].ToString();
        //                    }
        //                    intCheckLvDepends = !DBNull.Value.Equals(dv.Rows[II]["ANL"]) ?
        //                        1 : 1;
        //                }
        //            }
        //            if (intCheckLvDepends == 1)
        //            {
        //                strLvCode = strAnnlLvCode;
        //            }
        //            else
        //            {
        //                strLvCode = recallObj.LvCode;
        //            }

        //            DataTable dv1 = LeaveRecObj.LvSpentDays(recallObj.CompId, recallObj.RqstId, recallObj.RecallEmp, recallObj.AccountingYear);

        //            if (dv1.Rows.Count > 0)
        //            {
        //                intUnspent = int.Parse(dv1.Rows[0]["UNSPENT"].ToString());
        //            }

        //            // 'Update Leave Remaining Days Table With No. Of Days Remaining

        //            ////  string RmanDays = recallObj.spentDays

        //            //  varInsertRemainData[0] = recallObj.AccountingYear; 
        //            //  varInsertRemainData[1] = recallObj.RecallEmp; 
        //            //  varInsertRemainData[2] = recallObj; 
        //            //  varInsertRemainData[3] = recallObj.LvCode;  
        //            //  varInsertRemainData[4] = recallObj.RemainingDays; 
        //            //  varInsertRemainData[5] = recallObj.UserId; 
        //            //  varInsertRemainData[6] = recallObj.CompId; 

        //            //    IntResult = DataCreator.ExecuteProcedure("INSERT_H_EMP_LV_RMN_DAYS", varInsertRemainData);

        //            StrQuery = " UPDATE H_EMP_LV_RMN_DAYS SET H_LV_RMN_DAYS = H_LV_RMN_DAYS + " + intUnspent + " ,LV_RST_MDFD_BY = '" + recallObj.UserId + "'" +
        //                        " ,LV_RST_MDFD_DATE = SYSDATE  WHERE COMP_AID = '" + recallObj.CompId + "'" +
        //                        "  AND H_ACC_YEAR = '" + recallObj.AccountingYear + "' And H_EMPLYE_NMBR = " + recallObj.RecallEmp + " " +
        //                        " AND H_LV_GRP_CODE = " + recallObj.LvGrp + " AND H_LV_CODE = '" + recallObj.LvCode + "'";

        //            IntResult = DataCreator.ExecuteSQL(StrQuery);
        //        }

        //        string error;
        //        if (IntResult == 1) error = "";
        //        else error = "Error Saving Records";

        //        var response = new { Error = error };
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Log();
        //        throw new ExternalException();
        //    }
        //}

        public class LeaveRecClass
        {
            public string SpentDays { get; set; }
            public string AccountingYear { get; set; }
            public string LvCode { get; set; }
            public string LvGrp { get; set; }
            public string RqstId { get; set; }
            public string RecallEmp { get; set; }
            public string UserId { get; set; }
            public string CompId { get; set; }
            public string GRqstID { get; set; }
            public string RecallDt { get; set; }
            public string OldDuration { get; set; }
        }
    }
}
