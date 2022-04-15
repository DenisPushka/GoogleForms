using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GoogleForms
{
    internal static class Program
    {
        private static HttpClient _client;

        private static string Uri
        {
            get =>
                "https://docs.google.com/forms/d/e/1FAIpQLSdhRxs1EWYXtbD8wDZ4JucLfBTZMbQqX7EfrHQ6mjbup57zSQ/formResponse";
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        public static async Task Main()
        {
            GetLink();
            for (var i = 0; i < 15; i++)
            {
                _client = new HttpClient();
                var res = await _client.GetAsync(Uri);
                res.EnsureSuccessStatusCode();
                var responseBody = await res.Content.ReadAsStringAsync();
                var entryList = await Parsing(responseBody);
                var postData = FillingForm(entryList);

                var request = WebRequest.Create(Uri);
                var buff = HttpUtility.UrlPathEncode(postData);
                var data = Encoding.ASCII.GetBytes(buff);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                await using (var stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(data.AsMemory(0, data.Length));
                }

                try
                {
                    var response = (HttpWebResponse) await request.GetResponseAsync();
                    if (response.StatusCode == HttpStatusCode.OK)
                        Console.WriteLine("Данные успешно отправлены");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Произошла ошибка: {0}", e.Message);
                    throw;
                }
            }
        }

        private static void GetLink()
        {
            Console.WriteLine("Введите ссылку: ");
            Uri = Console.ReadLine();
            if (Uri != null)
                for (var i = Uri.Length - 1; 0 < i; i--)
                {
                    if (Uri[i].Equals('/'))
                    {
                        Uri += "formResponse";
                        break;
                    }

                    Uri = Uri.Remove(Uri.Length - 1);
                }
        }
        
        private static string FillingForm(List<string> entryList)
        {
            var postData = new StringBuilder();
            foreach (var entry in entryList)
            {
                var number = new StringBuilder();
                foreach (var t in entry.TakeWhile(t => !t.Equals(' ')))
                    number.Append(t);

                if (entry.Length > 10)
                {
                    var length = number.Length;
                    var input = entry.Substring(length + 1, entry.Length - length - 1);
                    postData.Append("&entry." + entry[..length] + "=" + input);
                    continue;
                }

                postData.Append("entry." + number + "=что-то непонятное");
                number.Clear();
            }

            return postData.ToString();
        }

        private static async Task<List<string>> Parsing(string responseBody)
        {
            const string dataParams = "data-params";
            var flag = false;
            var entryList = new List<string>();
            for (var i = 0; i < responseBody.Length - 12; i++)
            {
                if (responseBody[i].Equals(dataParams[0]))
                {
                    for (var j = 1; j < dataParams.Length - 1; j++)
                    {
                        i++;
                        if (!responseBody[i].Equals(dataParams[j]))
                        {
                            flag = false;
                            break;
                        }

                        flag = true;
                    }

                    if (flag)
                        entryList.Add(GetType(responseBody, i));
                }
            }

            return entryList;
        }

        private static string GetType(string responseBody, int i)
        {
            while (responseBody[i] != '[' || responseBody[i - 1] != '[')
                i++;

            i++;
            var buffer = new StringBuilder();
            while (responseBody[i] != ',')
            {
                buffer.Append(responseBody[i]);
                i++;
            }

            buffer.Append(' ');
            return GetT(responseBody, i, buffer.ToString());
        }

        private static string GetT(string responseBody, int i, string buff)
        {
            var b = i;
            while (responseBody.Substring(i - 3, 4) != "type")
                i++;

            if (responseBody.Substring(i + 3, 6) == "hidden")
            {
                // Выбор ответа
                for (var j = 0; j < 1; j++)
                {
                    while (responseBody.Substring(b, 6) != "&quot;")
                        b++;
                
                    b += 6;
                }
                
                while (responseBody.Substring(b, 6) != "&quot;")
                {
                    buff += responseBody[b];
                    b++;
                }
            }

            return buff;
        }
    }
}