using System;
using System.Data;
using System.Data.OracleClient;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PeoplePlusWebApi.Controllers
{
    public class UserProfileController : ApiController
    {
        private DataProvider.UserProfile _usrProfileObj = new DataProvider.UserProfile();
        public DataProvider.UserProfile UsrProfileObj
        {
            get { return _usrProfileObj; }
        }
        
        public HttpResponseMessage GetBasicDetails(string compId, int empNo)
        {
            try
            {
                DataTable dt = UsrProfileObj.GetSpecificRecords(compId, empNo);
                DataTable dt2 = UsrProfileObj.GetUserQualification(compId, empNo);
                DataTable dt3 = UsrProfileObj.GetUserProfCredential(compId, empNo);

                if (dt == null || dt2 == null || dt3 == null)
                    return Request.CreateResponse(HttpStatusCode.NoContent);

                var empTyeCode = dt.Rows[0]["HEMP_HETY_EMPLYE_TYPE_COD"] == null ? "" : dt.Rows[0]["HEMP_HETY_EMPLYE_TYPE_COD"].ToString();
                var supviorNo = dt.Rows[0]["HEMP_HEMP_EMPLYE_NMBR"] == null ? 0 : Convert.ToInt32(dt.Rows[0]["HEMP_HEMP_EMPLYE_NMBR"]);
                DataTable dt4 = UsrProfileObj.GetEmployeeType(empTyeCode);
                DataTable dt5 = UsrProfileObj.GetEmployeeReportedTo(supviorNo);

                if (dt4 == null || dt5 == null)
                    return Request.CreateResponse(HttpStatusCode.NoContent);

                var output = new { BasicDetails = dt, Qualifications = dt2, ProfessionalCredentials = dt3, EmployeeType = dt4, ReportTo = dt5 };
                return Request.CreateResponse(HttpStatusCode.OK, output);
            }
            catch (Exception ex)
            {
                ex.Log();
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }
        public HttpResponseMessage GetUserImage(int Id)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();

                DataTable dt = UsrProfileObj.GetUserImage(Id);

                if (dt == null || dt.Rows.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // string src = DBNull.Value.Equals(dt.Rows[0]["IMAGE_DATA"]) ? "~/Resource/Images/defaultImage.jpg" : dt.Rows[0]["IMAGE_DATA"].ToString();

                //src = HttpContext.Current.Server.MapPath(src);
                // src = Values.HostRootAddress + src.Remove(0, 2).Replace("/", "\\");

                byte[] imageByte = (byte[])dt.Rows[0]["IMAGE_DATA"];
                                        //try
                                        //{
                                        // imageByte = File.ReadAllBytes(src);
                                        //}
                                        //catch (Exception)
                                        //{
                                        // src = Values.RootAddress + "Resource\\Images\\defaultImage.jpg";
                                        // imageByte = File.ReadAllBytes(src);
                                        //}

                MemoryStream ms = new MemoryStream(imageByte);

                response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(ms);
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpg");

                return response;

            }
            catch (Exception ex)
            {
                ex.Log();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
        }


        public HttpResponseMessage PutImage(int empNo, string empName, string compId)
        {
            try
            {
                int status = 0;
                object[] varInsertPara = new Object[7];

                varInsertPara[0] = compId;
                varInsertPara[1] = empNo;
                varInsertPara[2] = "";
                varInsertPara[3] = empName;//Session("Username")
                varInsertPara[4] = DateTime.Now.ToString("dd-MMM-yyyy");
                varInsertPara[5] = empName;//Session("Username")
                varInsertPara[6] = DateTime.Now.ToString("dd-MMM-yyyy");

                int SavePhotoCount = DataCreator.ExecuteProcedure("INSERT_GM_EMP_LOGO", varInsertPara);

                byte[] picByte;
                using (Stream imageStream = Request.Content.ReadAsStreamAsync().Result)
                {
                    byte[] buffer = new byte[1024 * 1024];
                    int count = imageStream.Read(buffer, 0, buffer.Length);
                    picByte = new byte[count];
                    for (int i = 0; i < count; i++)
                    {
                        picByte[i] = buffer[i];
                    }
                }

                using (OracleConnection con = new OracleConnection(new DataProvider().GetConnectionString()))
                using (OracleCommand cmd = new OracleCommand())
                {
                    con.Open();
                    string query = "Update gm_emp_logo t set t.photo= :photo where t.emp_aid=:staffId and comp_aid=:compId";
                    cmd.Connection = con;
                    cmd.CommandText = query;
                    cmd.Parameters.AddWithValue("photo", picByte);
                    cmd.Parameters.AddWithValue("staffId", empNo);
                    cmd.Parameters.AddWithValue("compId", compId);
                    status = cmd.ExecuteNonQuery();
                }

                if (status == 1)
                    return Request.CreateResponse(new { ErrorStatus = 0 });
                else
                    return Request.CreateResponse(HttpStatusCode.OK, new { ErrorStatus = 1 });
            }
            catch (Exception ex)
            {
                ex.Log();
                return Request.CreateResponse(HttpStatusCode.OK, new { ErrorStatus = 1 });
            }

        }

        //public HttpResponseMessage GetUserImage(int Id)
        //{
        //    try
        //    {
        //        HttpResponseMessage response = new HttpResponseMessage();

        //        DataTable dt = UsrProfileObj.GetUserImage(Id);

        //        if (dt == null || dt.Rows.Count == 0)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NoContent);
        //        }
                
        //        string src = DBNull.Value.Equals(dt.Rows[0]["IMAGE_DATA"]) ? "~/Resource/Images/defaultImage.jpg" : dt.Rows[0]["IMAGE_DATA"].ToString(); 

        //        //src = HttpContext.Current.Server.MapPath(src);
        //        src = Values.HostRootAddress + src.Remove(0, 2).Replace("/", "\\");

        //        byte[] imageByte;
        //        if(File.Exists(src))
        //            imageByte = File.ReadAllBytes(src);
        //        else
        //        {
        //            src = Values.RootAddress + "Resource\\Images\\defaultImage.jpg";            //defaultImage.png for web version
        //            imageByte = File.ReadAllBytes(src);
        //        }

        //        MemoryStream ms = new MemoryStream(imageByte);

        //        response = new HttpResponseMessage(HttpStatusCode.OK);
        //        response.Content = new StreamContent(ms);
        //        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpg");

        //        return response;

        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Log();
        //        return Request.CreateResponse(HttpStatusCode.NoContent);
        //    }
        //}
        
        //public HttpResponseMessage PutImage(int empNo)
        //{
        //    try
        //    {
        //        //CREATE A SESSION ID FROM CURRENT DATE AND TIME CONCATENATED WITH EMP NO
        //        string sessionid = DateTime.Now.ToString();
        //        sessionid = sessionid.Replace("\\", "").Replace("/", "").Replace(" ", "").Replace(":", ""); //removes slash and the spaces
        //        sessionid += empNo;

        //        //GET PREVIOUS IMAGE LINK FROM THE DB
        //        DataTable dt = UsrProfileObj.GetUserImage(empNo);
        //        //if (dt == null || dt.Rows.Count == 0)
        //        //    return Request.CreateResponse(HttpStatusCode.OK, new { ErrorStatus = 1 });

        //        var src = DBNull.Value.Equals(dt.Rows[0]["IMAGE_DATA"]) ? "default" : dt.Rows[0]["IMAGE_DATA"].ToString();
                
        //        //check whether read image link is for default image or not
        //        string filename;
        //        if (src.ToLower().Contains("default"))
        //            filename = sessionid + DateTime.Now.Millisecond.ToString() + ".jpg";
        //        else
        //        {
        //            string[] output = src.Split('/');
        //            filename = output[output.GetUpperBound(output.Rank - 1)];
        //        }

        //        string path = Values.HostRootAddress + "Resource\\Images\\";
        //        //string path = HttpContext.Current.Server.MapPath("~/Resource/UploadedImages/");
        //        string fullpath = path + filename;

        //        //Request.Content.LoadIntoBufferAsync().Wait();

        //        using (Stream stream = Request.Content.ReadAsStreamAsync().Result)
        //        {
        //            Image image = Image.FromStream(stream);
        //            image.Save(fullpath);
        //        }

        //        object[] varSavePhoto = new object[5];
        //        varSavePhoto[0] = 0; //ID not necessary
        //        varSavePhoto[1] = "~/Resource/Images/" + filename;
        //        varSavePhoto[2] = ""; //FILE TYPE not necessary
        //        varSavePhoto[3] = empNo;
        //        varSavePhoto[4] = empNo;


        //        var Result = DataCreator.ExecuteProcedure("UPDATE_PASSPORT", varSavePhoto);


        //        if (Result == 0)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.OK, new { ErrorStatus = 0 });
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.OK, new { ErrorStatus = 1 });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Log();
        //        return Request.CreateResponse(HttpStatusCode.OK, new { ErrorStatus = 1 });
        //    }

        //}
    }
}
