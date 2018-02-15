using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.OracleClient;
using System.Data;
using System.IO;
using System.Data.OleDb ;

namespace SelfserviceAPI.DatabaseServices
{
    public class EmployeeDetails
    {

        public static string Connection()
        {
            /*
             * 
             * SERVER=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));
             * 
             */
            //return "Data Source=orcl;" + "Persist Security Info=False;User ID=neptune;" + "password=NEPTUNE";
            return "SERVER=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.152.2.27)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=neptune;Password=NEPTUNE;"; 
        }
        public static string Connection2()
        {
            return "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.152.2.27)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=neptune;Password=NEPTUNE;"; 
            //return "Provider=OraOLEDB.Oracle;Data Source=orcl;" + "Persist Security Info=False;User ID=neptune;" + "password=NEPTUNE";
        }

        public DataView ReturnDataview (string queryString)
        {
            var cn = Connection();

            var connection = new OracleConnection(cn);
            var command = connection.CreateCommand();

            var dv = new DataView();
            try
            {
                command.CommandText = queryString;
                var dataTable = new DataTable();

                connection.Open();

                var oracleDataAdapter = new OracleDataAdapter(command);
                oracleDataAdapter.Fill(dataTable);
                oracleDataAdapter.Dispose();

                dv  = new DataView(dataTable);

                if( dv == null) {
                    return null;
                }
                
            }
            
            catch  (Exception ex) {string e = ex.Message.ToString();}
            finally{connection.Close(); connection.Dispose(); command.Dispose();}

            return dv;
        }

        public int ReturnInteger(string queryString)
        {
            var cn = Connection();

            var connection = new OracleConnection(cn);
            var command = connection.CreateCommand();

            try
            {
                command.CommandText = queryString;
                
                connection.Open();
                
                return command.ExecuteNonQuery();

            }
            catch (Exception ex) { string e = ex.Message.ToString(); }
            finally { connection.Close(); connection.Dispose(); command.Dispose(); }

            return 0;
        }
        

        public DataView ProfileInfo(string userAID)
        {
            var query  = "select a.comp_aid,a.HEMP_EMPLYE_NMBR,a.HEMP_NEW_EMPLYE_NMBR,HEMP_EMPLYE_NAME,a.hemp_brth_date, b.USER_PASSWORD, a.HEMP_TITLE, a.HEMP_FLCT_LCTN_CODE,a.HEMP_HDPR_DPRTMNT_CODE, a.HEMP_LV_GRP_CODE,a.HEMP_HDDT_HGRD_GRDE_CODE, " +
                     " a.hemp_pin_nmbr,a.hemp_nssf_nmbr,a.hemp_acnt_nmbr, b.ROLE_ID,a.HEMP_EMAIL from h_emplye_mstr a,HRM_USER b  where a.comp_aid = b.comp_aid and a.HEMP_EMPLYE_NMBR = b.EMP_AID and  b.USER_AID ='" + userAID + "' and b.USER_STATUS='ACTIVE'";
           
            EmployeeDetails EmpDtls = new EmployeeDetails();
            DataView dv = EmpDtls.ReturnDataview(query);
            return dv;
        }

        public DataView ListOfTrainings(string ComCode)
        {
            var query = "SELECT A.HTPR_TRNG_PRGRM_CODE TrainingCode, B.HTRM_TRNG_DSCRPTN Description,A.HTPR_DRTN Duration, " +
            " A.HTPR_VENUE Venue,DECODE(B.HTRM_TYPE, 'G','GENERAL','S', 'SPECIFIC') Type ,A.HTPR_SRL_NMBR TrnSno " +
            " FROM H_TRNG_PRGRM_MSTR A, H_TRNG_MSTR B WHERE A.COMP_AID='" + ComCode +"'  AND A.HTPR_TRNG_PRGRM_CODE= B.HTRM_TRNG_CODE" ;

            DataView dv = new EmployeeDetails().ReturnDataview(query);
            return dv;
        }

        public DataView SubordinateEmpBaseOnTraining(string userAID,string TrngCode, string ComCode)
        {
            var query = "SELECT  HEMP_NEW_EMPLYE_NMBR EmployeeNumber ,HEMP_EMPLYE_NAME Name, HEMP_EMPLYE_NMBR StaffID, HDPR_DSCRPTN Department, HDSG_DSCRPTN Designation, HGRD_DSCRPTN Grade " +
            " FROM H_EMPLYE_MSTR A, H_DPRTMNT_MSTR B, H_DSGNTN_MSTR C, H_GRDE_MSTR D WHERE A.COMP_AID='" + ComCode + "' " +
            " AND HEMP_HDDT_HGRD_GRDE_CODE IN (SELECT HTRD_GRD_CODE FROM H_TRNG_DTLS WHERE COMP_AID='" + ComCode + "' AND HTRD_TRNG_CODE='" + TrngCode + "') AND HEMP_RSGNTN_DATE IS NULL " +
            " AND HEMP_EMPLYE_NMBR NOT IN (SELECT HTNM_HEMP_EMPLYE_NMBR FROM H_TRNG_NMNTN WHERE COMP_AID='" + ComCode + "' AND HTNM_TRNG_PRGRM_CODE='" + TrngCode + "') " +
            " AND HEMP_HEMP_EMPLYE_NMBR='" + userAID + "' " +
            " AND HEMP_HDDT_HGRD_GRDE_CODE = D.HGRD_GRDE_CODE AND HEMP_HDDT_HDSG_DSGNTN_COD = C.HDSG_DSGNTN_CODE " +
            " AND HEMP_HDPR_DPRTMNT_CODE = B.HDPR_DPRTMNT_CODE ORDER BY HEMP_EMPLYE_NAME ";

            DataView dv = new EmployeeDetails().ReturnDataview(query);
            return dv;
        }
                
        public DataView TrainingReqId(string CompCode, string TrnCode, int EmpNo, int TranSN)
        {
            var query  = " SELECT RQST_ID FROM H_TRNG_NMNTN WHERE  COMP_AID = '" + CompCode + "' And HTNM_HEMP_EMPLYE_NMBR = " + EmpNo +
                        " AND HTNM_TRNG_PRGRM_CODE ='" + TrnCode + "' AND    HTNM_TRNG_SRL_NMBR = " + TranSN ;
           
            EmployeeDetails EmpDtls = new EmployeeDetails();
            DataView dv = EmpDtls.ReturnDataview(query);
            return dv;
        }

        public int DeleteFromSSApprovalTable(string CompCode, object intGlobReqestID)
        {
            var query = "DELETE H_TRNG_NMNTN WHERE COMP_AID='" + CompCode + "' AND RQST_ID=" + intGlobReqestID ;

            var rowsAffected = (new EmployeeDetails()).ReturnInteger(query);
            return rowsAffected;
        }
        
        public DataView SelectSSGrievGLBMail(string CompCode, object GLBRqstID)
        {
            var query = " select b.appr_emp_aid , d.hemp_emplye_name approver, t.htnm_hemp_emplye_nmbr, c.hemp_emplye_name requester, d.hemp_email apprvr_email, " +
                            " c.hemp_email rqstr_email from H_TRNG_NMNTN t, ss_approval_employees b, h_emplye_mstr c, h_emplye_mstr d " +
                            " where t.comp_aid = b.comp_aid and t.comp_aid = d.comp_aid and t.comp_aid = c.comp_aid and t.rqst_id = b.rqst_id " +
                    " and c.hemp_emplye_nmbr = t.HTNM_HEMP_EMPLYE_NMBR and d.hemp_emplye_nmbr = b.appr_emp_aid" +
                    " and t.rqst_id = " + GLBRqstID + " and t.comp_aid ='" + CompCode + "'";

            DataView dv = (new EmployeeDetails()).ReturnDataview(query);
            return dv;
        }





        //for training approval page
        public DataView SelectViewTrngCurrStg(string CompCode, object ReqestID)
        {
            var query = "select HTNM_STS,RQST_CUR_STAGE from H_TRNG_NMNTN  WHERE COMP_AID ='" + CompCode + "' " +
                    " and RQST_ID =" + ReqestID ;

            DataView dv = (new EmployeeDetails()).ReturnDataview(query);
            return dv;
        }

        public DataView SelectTrainingApprvlPOP(string CompCode, object EmpNo)
        {
            var query = "SELECT A.HTNM_TRNG_SRL_NMBR SrNo ,A.HTNM_TRNG_PRGRM_CODE Training, B.HTRM_TRNG_DSCRPTN TrainingDescription, DECODE(A.HTNM_STS,'R','REQUEST') Status, " +
                    " A.HTNM_HEMP_EMPLYE_NMBR EmployeeNo,C.HEMP_EMPLYE_NAME EmployeeName,A.HTNM_HEMP_EMPLYE_NOMIBY NominatingEmpNo , " +
                    " D.HEMP_EMPLYE_NAME NominatingEmpName, A.HTNM_NMNTN_RSN Reason, A.HTNM_MDFD_DATE ModifiedDate, A.RQST_ID RequestID, A.RQST_CUR_STAGE CurrentStage " +
                    " FROM H_TRNG_NMNTN A,H_TRNG_MSTR B,H_EMPLYE_MSTR C,H_EMPLYE_MSTR D" +
                    " WHERE A.COMP_AID='" + CompCode + "'" +
                    " AND A.COMP_AID=B.COMP_AID" +
                    " AND A.COMP_AID=C.COMP_AID" +
                    " AND A.COMP_AID=D.COMP_AID" +
                    " AND A.HTNM_TRNG_PRGRM_CODE=B.HTRM_TRNG_CODE" +
                    " AND A.HTNM_HEMP_EMPLYE_NMBR=C.HEMP_EMPLYE_NMBR" +
                    " AND A.HTNM_HEMP_EMPLYE_NOMIBY=D.HEMP_EMPLYE_NMBR" +
                    " AND A.RQST_ID IN (SELECT RQST_ID FROM SS_APPROVAL_EMPLOYEES WHERE COMP_AID='" + CompCode + "' AND RQST_CODE='TRN' AND APPR_EMP_AID=" + EmpNo + ") " +
                    " ORDER BY HTNM_TRNG_PRGRM_CODE ";

            DataView dv = (new EmployeeDetails()).ReturnDataview(query);
            return dv;
        }







        public DataView SelectMailServerName(string CompCode)
        {
            var query = "select t.mail_svr_name from  gm_comp_hd t where t.comp_aid='" + CompCode + "'";

            DataView dv = (new EmployeeDetails()).ReturnDataview(query);
            return dv;
        }
        
        public int ExecuteProcedure(string StoredProcedureName, object[] ProcedureParameters)
        {
            try
            {
                var cn1 = Connection2();

                var sConnection = new  OleDbConnection();
                var sCommand = new OleDbCommand();
                var sDataAdapter = new OleDbDataAdapter();
                var rsAccess = new DataSet();
                int intI;
            
                sConnection = new OleDbConnection(cn1);
                sCommand = new OleDbCommand(StoredProcedureName, sConnection);
                sCommand.CommandType = CommandType.StoredProcedure;

                ProcedureParameters .GetUpperBound (0);
                ProcedureParameters.GetLowerBound (0);

                for (intI = ProcedureParameters.GetLowerBound (0); intI <= ProcedureParameters .GetUpperBound (0); intI++) 
                {
	                sCommand.Parameters.AddWithValue("param" + intI, ProcedureParameters[intI]).Direction = ParameterDirection.Input;
                }

                sCommand.Parameters.AddWithValue("sp_outnmbr", OleDbType.Integer).Direction = ParameterDirection.Output;

                if (sConnection .State == ConnectionState .Open)
                { 
                    sConnection .Close();
                }

                sConnection.Open();
                sDataAdapter = new OleDbDataAdapter(sCommand );

                if (sDataAdapter.Fill(rsAccess) == 0 )
                {
                    Int32 returnValue = Convert.ToInt32(sCommand.Parameters["sp_outnmbr"].Value);
                    sConnection.Close();
                    sConnection.Dispose();
                    return returnValue ;
                }

                return 0;
            }
            catch(Exception e)
            {
                string m = e.Message .ToString();
                return 1;
            }
        }

    }
}