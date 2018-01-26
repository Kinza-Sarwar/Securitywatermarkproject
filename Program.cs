using MPhillAssignment.LogData;
using Newtonsoft.Json;
using RestSharp;
using SendLog_Console.Logging;
using SendLog_Console.PostModel;
using SendLog_Console.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using C = SendLog_Console.Functions;

namespace SendLog_Console
{
    class Program
    {
        static Stopwatch stopwatch = new Stopwatch();
        static bool success = false;
        static bool LoggedIn = false;
        static void Main(string[] args)
        {
            try
            {
                Console.Clear();
                do
                {
                    Console.Clear();
                    C.Log("==================================================================");
                    C.Log("1. User Login:");
                    C.Log("2. Register New User:");
                    C.Log();
                    C.Log("0. Exit");
                    C.Log("==================================================================");
                    C.Write("Enter your Choice:\t");
                    string choice = C.Read();
                    int userChoice = 0;
                    if (int.TryParse(choice, out userChoice))
                    {
                        C.Log();
                        C.Log("==================================================================");
                        switch (userChoice)
                        {
                            case 0:
                                Environment.Exit(0);
                                break;
                            case 1:
                                UserLogin();
                                break;
                            case 2:
                                RegisterUser();
                                break;
                            default:
                                C.Log("You have entered a wrong choice. Please try again.");
                                break;
                        }
                        C.Log();
                        C.Log("Press any key to continue.................");
                        C.Read();
                    }
                    else
                    {
                        C.Log("You have entered a wrong choice. Please try again.");
                        C.Log();
                        C.Log("Press any key to continue.................");
                        C.Read();
                    }
                    Console.Clear();
                } while (!LoggedIn);

                //UserLogin();
                if (success)
                {
                    success = true;
                    Console.Clear();
                    do
                    {
                        C.Log("==================================================================");
                        C.Log("1. Save Log file to Logging Server:");
                        C.Log("2. Get Log file from Logging Server:");
                        C.Log("3. Update Log file to Logging Server:");
                        C.Log();
                        C.Log("0. Exit");
                        C.Log("==================================================================");
                        C.Write("Enter your Choice:\t");
                        string choice = C.Read();
                        int userChoice = 0;
                        if (int.TryParse(choice, out userChoice))
                        {
                            C.Log();
                            C.Log("==================================================================");
                            switch (userChoice)
                            {
                                case 0:
                                    Environment.Exit(0);
                                    break;
                                case 1:
                                    SaveFile();
                                    break;
                                case 2:
                                    GetLogFile();
                                    break;
                                case 3:
                                    UpdateFile();
                                    break;
                                default:
                                    C.Log("You have entered a wrong choice. Please try again.");
                                    break;
                            }
                            C.Log();
                            C.Log("Press any key to continue.................");
                            C.Read();
                        }
                        else
                        {
                            C.Log("You have entered a wrong choice. Please try again.");
                            C.Log();
                            C.Log("Press any key to continue.................");
                            C.Read();
                        }
                        Console.Clear();
                    } while (true);
                }
                C.Read();
            }
            catch (Exception ex)
            {
                C.Log("============================ERROR============================");
                C.Log(ex.Message);
                C.Read();
            }
        }

        private static void UserLogin()
        {
            AccessToken userData = null;
            do
            {
                success = true;
                C.Write("Username:\t");
                string userName = C.Read();
                C.Write("Password:\t");
                string password = C.ReadPassword();

                stopwatch.Restart();
                //AccessToken data = C.CallService<AccessToken, object>(Method.POST, Url.Login, login, HttpHeaders.FormUrlEncoded, false).Result;
                userData = C.Login(userName, password).Result;
                stopwatch.Stop();
                C.LogExecutionTime("User Login", stopwatch);

                if (userData != null)
                {
                    C.Log("Login Successfull");
                    success = true;
                }
                else
                {
                    C.Log("Username or Password is incorrect");
                    success = false;
                    C.Read();
                }
                C.Log();
                C.Log("========================================================");
                C.Log();
            } while (!success);

            if (success)
            {
                if (userData.IsActive)
                {
                    Services.AccessToken = userData.Access;
                    Encryption.PublicKey = userData.PublicKey;
                    LoggedIn = true;
                }
                else
                {
                    success = false;
                    Services.AccessToken = userData.Access;
                    C.Log();
                    C.Log("========================================================");
                    C.Log("You have been deactived from the system.");
                    C.Log("========================================================");
                }
            }
        }

        private static void SaveFile()
        {
            do
            {
                success = true;
                C.Write("Enter log file Path:\t");
                string fileName = C.Read();

                StreamReader re = File.OpenText(fileName);
                string fileData = re.ReadToEndAsync().Result;

                if (fileData.Substring(fileData.IndexOf("}") + 1, 1) != ",")
                {
                    fileData = "[" + fileData.Replace("}", "},") + "]";
                }

                List<Log_Data> data = JsonConvert.DeserializeObject<List<Log_Data>>(fileData);
                C.Log("Saving Log to Logging Server");
                List<Log_Data> eData = new List<Log_Data>();

                stopwatch.Restart();
                foreach (var item in data)
                {
                    var d = item.EncryptData(Encryption.PublicKey);
                    eData.Add(d);
                }
                stopwatch.Stop();
                C.LogExecutionTime("SaveFile: Encrypting File", stopwatch);

                stopwatch.Restart();
                bool saveData = C.CallService<bool, List<Log_Data>>(Method.POST, Url.SaveLog, eData, HttpHeaders.Json, true).Result;
                stopwatch.Stop();
                C.LogExecutionTime("SaveFile: Uploading File", stopwatch);
                if (saveData)
                {
                    success = true;
                    C.Log();
                    C.Log("========================================================");
                    C.Log("Log Uploaded Successfully");
                    C.Log("========================================================");
                    C.Log("========================================================");
                }
                else
                {
                    success = false;
                    C.Log("========================================================");
                    C.Log("Error: Please try again.");
                    C.Log("========================================================");
                    C.Read();
                }
            } while (!success);
        }

        private static void GetLogFile()
        {
            do
            {
                success = true;
                C.Write("Please Enter Username :\t");
                string userEmail = C.Read();

                stopwatch.Restart();
                ApiData<List<Log_Data>> logData = C.CallService<ApiData<List<Log_Data>>, string>(Method.GET, Url.GetLog, true, "email".PairedWith(userEmail)).Result;
                stopwatch.Stop();
                C.LogExecutionTime("GetLogFile: Download File", stopwatch);
                if (logData.Status)
                {
                    success = true;
                    stopwatch.Restart();
                    C.Save_ApiLogData(logData.Data);
                    stopwatch.Stop();
                    C.LogExecutionTime("GetLogFile: Save File", stopwatch);
                    C.Log();
                    C.Log("========================================================");
                    C.Log("Log Saved Successfully");
                    C.Log("========================================================");
                    C.Log("========================================================");
                }
                else
                {
                    success = false;
                    C.Log("========================================================");
                    C.Log(logData.Message);
                    C.Log("========================================================");
                    C.Read();
                }
            } while (!success);
        }

        private static void UpdateFile()
        {
            do
            {
                success = true;
                C.Write("Enter log file Path:\t");
                string fileName = C.Read();

                StreamReader re = File.OpenText(fileName);
                string fileData = re.ReadToEndAsync().Result;

                if (fileData.Substring(fileData.IndexOf("}") + 1, 1) != ",")
                {
                    fileData = "[" + fileData.Replace("}", "},") + "]";
                }

                List<Log_Data> data = JsonConvert.DeserializeObject<List<Log_Data>>(fileData);
                C.Log("Saving Log to Logging Server");
                List<Log_Data> eData = new List<Log_Data>();

                stopwatch.Restart();
                foreach (var item in data)
                {
                    var d = item.EncryptData(Encryption.PublicKey);
                    d.LogData_Id = Encryption.DecryptText(d.LogData_Id, Encryption.PublicKey);
                    eData.Add(d);
                }
                stopwatch.Stop();
                C.LogExecutionTime("UpdateFile: Encrypt File", stopwatch);

                stopwatch.Restart();
                bool saveData = C.CallService<bool, List<Log_Data>>(Method.POST, Url.UpdateLog, eData, HttpHeaders.Json, true).Result;
                stopwatch.Stop();
                C.LogExecutionTime("UpdateFile: Upload File", stopwatch);
                if (saveData)
                {
                    success = true;
                    C.Log();
                    C.Log("========================================================");
                    C.Log("Log Uploaded Successfully");
                    C.Log("========================================================");
                    C.Log("========================================================");
                }
                else
                {
                    success = false;
                    C.Log("========================================================");
                    C.Log("Error: Please try again.");
                    C.Log("========================================================");
                    C.Read();
                }
            } while (!success);
        }

        private static void RegisterUser()
        {
            do
            {
                success = true;
                C.Write("Username:\t");
                string userName = C.Read();
                C.Write("Password:\t");
                string password = C.ReadPassword();

                stopwatch.Restart();
                //AccessToken data = C.CallService<AccessToken, object>(Method.POST, Url.Login, login, HttpHeaders.FormUrlEncoded, false).Result;
                string registerData = C.Register(userName, password).Result;
                stopwatch.Stop();
                C.LogExecutionTime("Register new user", stopwatch);

                if (!string.IsNullOrEmpty(registerData))
                {
                    C.Log(registerData);
                    success = true;
                }
                else
                {
                    C.Log(registerData);
                    success = false;
                }
                C.Log();
                C.Log("========================================================");
                C.Log();
            } while (!success);
        }

    }
}
