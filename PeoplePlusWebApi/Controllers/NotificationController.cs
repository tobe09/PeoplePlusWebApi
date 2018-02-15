using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace PeoplePlusWebApi.Controllers
{
    public class NotificationController : ApiController
    {
        private DataProvider.Notification _notifObj = new DataProvider.Notification();

        public class NotifNewId
        {
            public string CompId { get; set; }
            public string DeviceId { get; set; }
            public int EmpNo { get; set; }
            public string UserId { get; set; }
        }

        [Route("api/Notification/PostNewDevId")]
        public object PostNewDevId([FromBody] NotifNewId notifNewIdObj)
        {
            try
            {
                object[] insertDevId = new object[1];
                insertDevId[0] = notifNewIdObj.DeviceId;

                int status = DataCreator.ExecuteProcedure("INSERT_NOTIF_FCM_DEVICE_ID", insertDevId);

                return new { Status = status };
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        [Route("api/Notification/PostPreLogin")]
        public object PostPreLogin([FromBody] NotifNewId notifNewIdObj)
        {
            try
            {
                int status = 0;
                if (!string.IsNullOrEmpty(notifNewIdObj.DeviceId))
                {
                    object[] insertNewId = new object[4];
                    insertNewId[0] = notifNewIdObj.CompId;
                    insertNewId[1] = notifNewIdObj.DeviceId;
                    insertNewId[2] = notifNewIdObj.EmpNo;
                    insertNewId[3] = notifNewIdObj.UserId;

                    status = DataCreator.ExecuteProcedure("INSERT_NOTIF_DEVICE_ID", insertNewId);        //INSERT NEW ID FOR USER
                }

                DataTable outstandingMsgDt = _notifObj.GetOustandingNotifications(notifNewIdObj.CompId, notifNewIdObj.EmpNo);

                //NOT USED SO AS TO PERSIST NOTIFICATIONS
                //object[] updateNotifMsg = new object[3];
                //updateNotifMsg[0] = notifNewIdObj.CompId;
                //updateNotifMsg[1] = notifNewIdObj.UserId;
                //updateNotifMsg[2] = notifNewIdObj.UserId;

                //status = DataCreator.ExecuteProcedure("UPDATE_NOTIF_MSG_TEMP", updateNotifMsg);          //UPDATE SENT NOTIFICATIONS IN TABLE

                return new { Status = status, Values = outstandingMsgDt };
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        [Route("api/Notification/PostPreLogout")]
        public object PostPreLogout([FromBody] NotifNewId notifNewIdObj)
        {
            try
            {
                int status = 0;
                if (!string.IsNullOrEmpty(notifNewIdObj.DeviceId))
                {
                    object[] insertRemoveId = new object[4];
                    insertRemoveId[0] = notifNewIdObj.CompId;
                    insertRemoveId[1] = notifNewIdObj.DeviceId;
                    insertRemoveId[2] = notifNewIdObj.EmpNo;
                    insertRemoveId[3] = notifNewIdObj.UserId;

                    status = DataCreator.ExecuteProcedure("DELETE_NOTIF_DEVICE_ID", insertRemoveId);
                }

                return new { Status = status };
            }
            catch (Exception ex)
            {
                ex.Log();
                throw new ExternalException();
            }
        }

        public void SendNotification(string compId, int rqstId, int recvrEmpNo, string userId, string subject, string body)
        {
            object[] insertMsgTemp = new object[6];
            insertMsgTemp[0] = compId;
            insertMsgTemp[1] = recvrEmpNo;
            insertMsgTemp[2] = rqstId;
            insertMsgTemp[3] = subject;
            insertMsgTemp[4] = body;
            insertMsgTemp[5] = userId;

            int status = DataCreator.ExecuteProcedure("INSERT_NOTIF_MSG_TEMP", insertMsgTemp);

            DataTable devIdDt = _notifObj.GetDeviceIdForUser(compId, recvrEmpNo);

            if (devIdDt.Rows.Count > 0)
            {
                for (int i = 0; i < devIdDt.Rows.Count; i++)
                {
                    string devId = devIdDt.Rows[i]["DEVICE_ID"].ToString();
                    PostToFcm(devId, new[] { subject, body });
                }
            }
        }

        public void PostToFcm(string deviceKey, string[] msgInfo)
        {
            try
            {
                object msgObj = new
                {
                    to = deviceKey,
                    data = new { title = msgInfo[0], body = msgInfo[1] },
                    notification = new { title = msgInfo[0], body = msgInfo[1] }
                };

                JObject JObjMsg = JObject.FromObject(msgObj);
                Uri fcmUri = new Uri("https://fcm.googleapis.com/fcm/send");
                string apiKey = "AAAAH97E2_c:APA91bHPqqjvTzzXDRaGHfVfOOhOJzt2KR7XOZKHCg0IBrVLT7IdysNMa6NFjuGq7Gru5yFGn-" +
                                "00SEWRA3Iiv0ut25A2jvN3-N8szfeADac307mBY4OVspxvwAeCfqDJidc71Wx6yM6n";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "key=" + apiKey);

                client.PostAsync(fcmUri, new StringContent(JObjMsg.ToString(), Encoding.Default, "application/json")).ContinueWith(resp =>
                {
                    JObject result = JObject.Parse(resp.Result.Content.ReadAsStringAsync().Result);

                    int success = ((FcmStatus)result.ToObject(typeof(FcmStatus))).success;      //if success > 0 then success, else failure (when success = 0)
                });
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        public class FcmStatus
        {
            public int success { get; set; }
        }
    }
}
