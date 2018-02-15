using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;

namespace PeoplePlusWebApi
{
    public class CommonMethodsClass
    {

        public int SendAlerts(string strComp, string strSender, int? intRecipientId, string strMessage, string apprReqType, int formID, int requestId, bool isDefault=true)
        {
            int status;

            string strAc;
            switch (apprReqType.ToLower())
            {
                case "approvals":
                    strAc = "APR";
                    break;
                case "birthdays":
                    strAc = "BIR";
                    break;
                case "holidays":
                    strAc = "HOL";
                    break;
                default:
                    strAc = apprReqType;
                    break;
            }

            object[] insertPara = new object[11];
            insertPara[0] = strComp;
            insertPara[1] = strAc;
            insertPara[2] = strMessage;
            insertPara[3] = intRecipientId == 0 ? null : intRecipientId;
            insertPara[4] = strAc == "BIR" || strAc == "HOL" ? "A" : "R";
            insertPara[5] = strSender;
            insertPara[6] = 1;
            insertPara[7] = "";
            insertPara[8] = 1;
            insertPara[9] = DateTime.Now.ToString("dd/MMM/yyyy");
            insertPara[10] = requestId; 
                

            if (isDefault)
            {
                status = DataCreator.ExecuteProcedure("INSERT_GM_ALERTS", insertPara);

                if (status == 0)
                {
                    object[] insertMapPara = new object[2];
                    insertMapPara[0] = formID;
                    insertMapPara[1] = strSender;
                    status = DataCreator.ExecuteProcedure("INSERT_SS_ALERTID_MAP", insertMapPara);
                }
            }
            else
            {
                status = DataCreator.ExecuteProcedure("INSERT_GM_ALERTSME", insertPara);
            }

            return status;
        }


        public int DismissAlert(string compId, string userId, int alertId, int requestId)
        {
            object[] updatePara = new object[6];

            updatePara[0] = compId;
            updatePara[1] = alertId;
            updatePara[2] = 0;
            updatePara[3] = "D";
            updatePara[4] = userId;
            updatePara[5] = requestId;

            int status = DataCreator.ExecuteProcedure("UPDATE_GM_ALERTS", updatePara);

            return status;
        }
        

        public string ZeroPad(string value)
        {
            string newValue = "0000000000000" + value;
            newValue = newValue.Substring(newValue.Length - 6);
            return newValue;
        }
        

        public void MailRoutines(string recieverAddr, string subject, string body, string compCode)
        {
            try
            {
                DataTable mailDt = new DataProvider.MedicalRequest().GetLeaveApprovalMail(compCode);

                if (mailDt.Rows.Count > 0)
                {
                    MailMessage myMessage = new MailMessage();
                    myMessage.To.Add(new MailAddress(recieverAddr));
                    myMessage.Subject = subject;
                    myMessage.Body = body;
                    myMessage.IsBodyHtml = true;
                    SmtpClient mySender = new SmtpClient();

                    string mailServerName = DBNull.Value.Equals(mailDt.Rows[0]["mail_svr_name"]) ? "" : mailDt.Rows[0]["mail_svr_name"].ToString();
                    //use config file settings to send message if no email server available 
                    if (!string.IsNullOrEmpty(mailServerName))
                    {
                        mySender.Host = mailServerName;
                        mySender.Port = 587;
                    }

                    //mySender.Send(myMessage);  disabled due to database email address activity
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }


        public int GetWorkDays(string strFromDate, string strToDate, string compId)
        {
            int noOfDays, intRecHols = 0, intNonRecHols = 0, workDays = 0;

            string[] strFromDateArray = strFromDate.Split('/');
            string[] strToDateArray = strToDate.Split('/');
            DateTime fromDate = new DateTime(int.Parse(strFromDateArray[2]), int.Parse(strFromDateArray[1]), int.Parse(strFromDateArray[0]));
            DateTime toDate = new DateTime(int.Parse(strToDateArray[2]), int.Parse(strToDateArray[1]), int.Parse(strToDateArray[0]));
            //DateTime fromDate = DateTime.ParseExact(strFromDate, "dd/MM/yyyy", null);
            //DateTime toDate = DateTime.ParseExact(strToDate, "dd/MM/yyyy", null);

            noOfDays = (toDate - fromDate).Days;
            object[] workDaysArray, recHolArray, nonRecHolsArray;

            //'**************************
            //'Get Working Days in a Week
            DataTable workDaysDt = new DataProvider.Approval().GetWorkDays(compId);
            if (workDaysDt.Rows.Count > 0)
            {
                workDaysArray = new object[workDaysDt.Rows.Count];
                for (int i = 0; i < workDaysDt.Rows.Count; i++)
                {
                    workDaysArray[i] = workDaysDt.Rows[i]["WorkDays"];
                }
            }
            else
            {
                workDaysArray = new object[5] { 1, 2, 3, 4, 5 };
            }

            //'*******************************
            //'Check Recurring Public Holidays
            //'*******************************
            DataTable holidayDt = new DataProvider.Approval().GetRecurringHols();
            if (holidayDt.Rows.Count > 0)
            {
                recHolArray = new object[holidayDt.Rows.Count];
                for (int i = 0; i < holidayDt.Rows.Count; i++)
                {
                    recHolArray[i] = holidayDt.Rows[i]["RecHols"];
                }
            }
            else
            {
                recHolArray = new object[1] { new DateTime() };
            }

            //'***********************************
            //'Check Non-Recurring Public Holidays
            //'***********************************
            DataTable nonRecHolidayDt = new DataProvider.Approval().GetNonRecurringHols();
            if (nonRecHolidayDt.Rows.Count > 0)
            {
                nonRecHolsArray = new object[nonRecHolidayDt.Rows.Count];
                for (int i = 0; i < nonRecHolidayDt.Rows.Count; i++)
                {
                    nonRecHolsArray[i] = nonRecHolidayDt.Rows[i]["NonRecHols"];
                }
            }
            else
            {
                nonRecHolsArray = new object[1] { new DateTime() };
            }

            //'************************
            //'Get No. of working days
            //'************************
            Dictionary<DayOfWeek, int> weekDayList = new Dictionary<DayOfWeek, int>();
            weekDayList.Add(DayOfWeek.Monday, 1);
            weekDayList.Add(DayOfWeek.Tuesday, 2);
            weekDayList.Add(DayOfWeek.Wednesday, 3);
            weekDayList.Add(DayOfWeek.Thursday, 4);
            weekDayList.Add(DayOfWeek.Friday, 5);
            weekDayList.Add(DayOfWeek.Saturday, 6);
            weekDayList.Add(DayOfWeek.Sunday, 7);

            for (int i = 0; i < noOfDays; i++)
            {
                DateTime iDate = fromDate.Add(new TimeSpan(i, 0, 0, 0));
                DayOfWeek dayOfWeek = iDate.DayOfWeek;

                for (int j = 0; j < workDaysArray.Length; j++)
                {
                    if (int.Parse(workDaysArray[j].ToString()) == weekDayList[dayOfWeek])
                    {
                        //check date from db and convert appropraitely
                        string strDay = iDate.ToString().Substring(0, 6).Replace('/', ' ').ToUpper();

                        //for recurring holidays count
                        for (int k = 0; k < recHolArray.Length; k++)
                        {
                            if ((string)recHolArray[k] == strDay)
                            {
                                intRecHols++;
                                break;
                            }
                        }

                        //for non-recurring holidays count
                        for (int k = 0; k < nonRecHolsArray.Length; k++)
                        {
                            if (nonRecHolsArray[k].ToString().Substring(0, 6).Replace('/', ' ').ToUpper() == strDay)
                            {
                                intNonRecHols++;
                                break;
                            }
                        }
                        workDays++;
                        break;
                    }
                }
            }

            //check if holidays are payable days
            DataTable holsPayableDt = new DataProvider.Approval().GetPayHolidayStatus(compId);
            string holsPayable = "N";               //DEFAULT STATUS 

            if (holsPayableDt.Rows.Count > 0)
            {
                holsPayable = (string)holsPayableDt.Rows[0]["Value"];
            }

            if (holsPayable != "Y")
            {
                workDays -= (intRecHols + intNonRecHols);
            }

            return workDays;
        }


        public DateTime? ConvertStringToDate(string dateStr)
        {
            try
            {
                int[] dateArr = Array.ConvertAll(dateStr.Split('/'), int.Parse);
                int year = dateArr[2];
                int month = dateArr[1];
                int day = dateArr[0];
                DateTime date = new DateTime(year, month, day);
                return date;
            }
            catch
            {
                return null;
            }
        }

    }
}