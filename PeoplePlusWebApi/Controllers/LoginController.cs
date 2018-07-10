using System;
using System.Web.Http;
using System.Data;

namespace PeoplePlusWebApi.Controllers
{
    public class LoginController : ApiController
    {
        DataProvider.Login LoginObj { get; } = new DataProvider.Login();

        public User PostUserLogin([FromBody] LoginClass loginObj)
        {
            try
            {
                User CurrentUser = new User();

                string id = loginObj.UserId.ToUpper();
                string password = loginObj.Password;

                //check for unsecure text in login credentials
                if (DataProvider.sqlProtect(id))
                {
                    DataTable userDt = LoginObj.GetUserDetails(id);             //get user's basic details
                    //check if username exists
                    if (userDt.Rows.Count > 0)
                    {
                        string existPassword = (string)userDt.Rows[0]["USER_PASSWORD"];         //get user encrypted password
                        string compId = (string)userDt.Rows[0]["COMP_AID"];                     //get company id to check license
                        int empNo = int.Parse(userDt.Rows[0]["HEMP_EMPLYE_NMBR"].ToString());   //get employee number
                        //check password
                        if (password == existPassword)
                        {
                            DataTable licenseDateDt = LoginObj.GetLicenseDate(compId);
                            string licenseDateStr = (string)licenseDateDt.Rows[0]["EXPIRY_DATE"];
                            licenseDateStr = new DataProvider().DecryptText(licenseDateStr);
                            DateTime licenseDate = DateTime.Parse(licenseDateStr);
                            //check license 
                            if (licenseDate >= DateTime.Now)
                            {
                                DataTable compNameDt = LoginObj.GetComputerCode(compId);
                                DataTable accountYearDt = LoginObj.GetAccountYear(compId);
                                DataTable companyEmailDt = LoginObj.GetCompanyEmail(compId);
                                DataTable extraDataDt = LoginObj.GetExtraData(empNo);

                                CurrentUser = PopulateUserInfo(compNameDt, userDt, accountYearDt, extraDataDt, licenseDate);
                            }
                            else
                            {
                                CurrentUser.Error = "Expired License";
                            }
                        }
                        else
                        {
                            CurrentUser.Error = "Invalid Username/Password";
                        }
                    }
                    else
                    {
                        CurrentUser.Error = "Invalid Username/Password";
                    }
                }
                else
                {
                    CurrentUser.Error = "Unsecure Text Entry";
                }

                return CurrentUser;
            }

            catch (Exception ex)
            {
                ex.Log();                           //log exception to file
                throw new ExternalException();      //throw formatted exception
            }
        }

        private User PopulateUserInfo(DataTable compNameDt, DataTable userDt, DataTable accountYearDt, DataTable extraDataDt, DateTime licenseDate)
        {
            User CurrentUser = new User();

            if (compNameDt.Rows.Count > 0)
            {
                CurrentUser.CompName = DBNull.Value.Equals(compNameDt.Rows[0]["COMP_NAME"]) ? "" : (string)compNameDt.Rows[0]["COMP_NAME"];
            }
            else
            {
                CurrentUser.CompName = "";
            }

            if (userDt.Rows.Count > 0)
            {
                CurrentUser.CompId = (string)userDt.Rows[0]["comp_aid"];
                CurrentUser.RoleId = (string)userDt.Rows[0]["ROLE_ID"];
                CurrentUser.EmployeeNo = int.Parse(userDt.Rows[0]["HEMP_EMPLYE_NMBR"].ToString());
                CurrentUser.Name = (string)userDt.Rows[0]["HEMP_EMPLYE_NAME"];
                CurrentUser.Title = DBNull.Value.Equals(userDt.Rows[0]["HEMP_TITLE"]) ? "" : (string)userDt.Rows[0]["HEMP_TITLE"];
                CurrentUser.DateOfBirth = DBNull.Value.Equals(userDt.Rows[0]["hemp_brth_date"]) ? new DateTime() : (DateTime)userDt.Rows[0]["hemp_brth_date"];
                CurrentUser.AccountNo = DBNull.Value.Equals(userDt.Rows[0]["hemp_acnt_nmbr"]) ? "" : (string)userDt.Rows[0]["hemp_acnt_nmbr"];
                CurrentUser.Email = DBNull.Value.Equals(userDt.Rows[0]["HEMP_EMAIL"]) ? "" : (string)userDt.Rows[0]["HEMP_EMAIL"];
                CurrentUser.DeptCode = DBNull.Value.Equals(userDt.Rows[0]["HEMP_HDPR_DPRTMNT_CODE"]) ? "" : (string)userDt.Rows[0]["HEMP_HDPR_DPRTMNT_CODE"];
                CurrentUser.GradeCode = DBNull.Value.Equals(userDt.Rows[0]["HEMP_HDDT_HGRD_GRDE_CODE"]) ? "" : (string)userDt.Rows[0]["HEMP_HDDT_HGRD_GRDE_CODE"];
                CurrentUser.LocationCode= DBNull.Value.Equals(userDt.Rows[0]["HEMP_FLCT_LCTN_CODE"]) ? "" : (string)userDt.Rows[0]["HEMP_FLCT_LCTN_CODE"]; 
            }
            else
            {
                CurrentUser.CompId = "";
                CurrentUser.RoleId = "";
                CurrentUser.EmployeeNo = 0;
                CurrentUser.Name = "";
                CurrentUser.Title = "";
                CurrentUser.DateOfBirth = DateTime.Now;
                CurrentUser.AccountNo = "";
                CurrentUser.Email = "";
                CurrentUser.DeptCode = "";
                CurrentUser.GradeCode = "";
            }

            CurrentUser.LicenseDate = licenseDate;

            if (accountYearDt.Rows.Count > 0)
            {
                CurrentUser.AccountingYear = DBNull.Value.Equals(accountYearDt.Rows[0]["acc_year"]) ? "" : (string)accountYearDt.Rows[0]["acc_year"];
            }
            else
            {
                CurrentUser.AccountingYear = "";
            }
            
            if(extraDataDt.Rows.Count>0)
            {
                CurrentUser.Location = DBNull.Value.Equals(extraDataDt.Rows[0]["flct_dscrptn"]) ? "" : (string)extraDataDt.Rows[0]["flct_dscrptn"];
                CurrentUser.Department = DBNull.Value.Equals(extraDataDt.Rows[0]["hdpr_dscrptn"]) ? "" : (string)extraDataDt.Rows[0]["hdpr_dscrptn"];
                CurrentUser.Designation = DBNull.Value.Equals(extraDataDt.Rows[0]["hdsg_dscrptn"]) ? "" : (string)extraDataDt.Rows[0]["hdsg_dscrptn"];
                CurrentUser.Grade = DBNull.Value.Equals(extraDataDt.Rows[0]["HGRD_DSCRPTN"]) ? "" : (string)extraDataDt.Rows[0]["HGRD_DSCRPTN"];
            }
            else
            {
                CurrentUser.Location = "";
                CurrentUser.Department = "";
                CurrentUser.Designation = "";
                CurrentUser.Grade = "";
            }

            CurrentUser.Error = "";

            return CurrentUser;
        }

        [Route("api/Login/GetNetworkStatus")]
        public object GetNetworkStatus()
        {
            return new { Error = "" };
        }

        public class LoginClass
        {
            public string UserId { get; set; }
            public string Password { get; set; }
        }
    }
}
