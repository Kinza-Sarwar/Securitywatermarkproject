using RestSharp;
using SendLog_Console.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SendLog_Console.PostModel;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using SendLog_Console.Logging;
using Newtonsoft.Json;
using MPhillAssignment.LogData;

namespace SendLog_Console
{
    public static class Functions
    {
        private static string ExecutionLogFile = ConfigurationManager.AppSettings["ExecutionLogFile_Path"].ToString();
        private static string LogFile = ConfigurationManager.AppSettings["LogFile_Path"].ToString();

        public static KeyValuePair<string, T[]> PairedWith<T>(this string key, T[] value)
        {
            return new KeyValuePair<string, T[]>(key, value);
        }

        public static KeyValuePair<string, T> PairedWith<T>(this string key, T value)
        {
            return new KeyValuePair<string, T>(key, value);
        }

        public static void Log(object value = null)
        {
            Console.WriteLine(value);
        }

        public static void Write(string text)
        {
            Console.Write(text);
        }

        public static string Read()
        {
            return Console.ReadLine();
        }

        public static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            // add a new line because user pressed enter at the end of their password
            Console.WriteLine();
            return password;
        }

        public static async Task<AccessToken> Login(string username, string password)
        {
            Login_PostModel data = new Login_PostModel { GrantType = "password", UserName = username.ToLower(), Password = password };
            IRestResponse<string> response = await new Services().ExecuteService<string, Login_PostModel>(Method.POST, Url.Login, data, HttpHeaders.FormUrlEncoded, false);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<AccessToken>(response.Data);
            }
            else
            {
                return null;
            }
        }

        public static async Task<string> Register(string username, string password)
        {
            IRestResponse<string> response = await new Services().ExecuteService<string, int>(Method.GET, string.Format("{0}/{1}/{2}", Url.Register, username, password));
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.Data;
            }
            else
            {
                return null;
            }
        }

        public static async Task<T> CallService<T, Q>(Method method, string resource, bool authenticate = true, params KeyValuePair<string, Q>[] query) where T : new()
        {
            IRestResponse<string> response = await new Services().ExecuteService<string, Q>(method, resource, authenticate, query);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<T>(response.Data);
            }
            else
            {
                return default(T);
            }
        }

        public static async Task<T> CallService<T, Q>(Method method, string resource, Q data, string httpHeaders = "", bool authenticate = true) where T : new()
        {
            try
            {
                IRestResponse<T> response = await new Services().ExecuteService<T, Q>(method, resource, data, httpHeaders, authenticate);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return response.Data;
                }
                else
                {
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string GetTimeString(this Stopwatch stopwatch, int numberofDigits = 1)
        {
            double time = stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
            if (time > 1)
                return Math.Round(time, numberofDigits) + " s";
            if (time > 1e-3)
                return Math.Round(1e3 * time, numberofDigits) + " ms";
            if (time > 1e-6)
                return Math.Round(1e6 * time, numberofDigits) + " µs";
            if (time > 1e-9)
                return Math.Round(1e9 * time, numberofDigits) + " ns";
            return stopwatch.ElapsedTicks + " ticks";
        }

        private static void Log(string functionName, string time, TextWriter w)
        {
            w.Write("\r\nLog Entry:\t");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("Function Name:\t{0}", functionName);
            w.WriteLine("Execution Time:\t{0}", time);
            w.WriteLine("--------------------------------------------------------------");
        }

        public static void LogExecutionTime(string functionName, Stopwatch sWatch)
        {
            //if (!File.Exists(ExecutionLogFile))
            //{
            //    var stream = File.Create(ExecutionLogFile);
            //    stream.Close();
            //}
            using (StreamWriter w = File.CreateText(ExecutionLogFile))
            {
                Log(functionName, sWatch.GetTimeString(), w);
            }
        }

        public static void Save_ApiLogData(List<Log_Data> data)
        {
            using (StreamWriter w = File.CreateText(LogFile))
            {
                foreach (var item in data)
                {
                    Log_Data d = item.DecryptData(Encryption.PublicKey);
                    d.LogData_Id = Encryption.EncryptText(d.LogData_Id.ToString(), Encryption.PublicKey);
                    string jsonData = JsonConvert.SerializeObject(d);
                    w.Write(jsonData);
                }
            }
        }

    }
}
