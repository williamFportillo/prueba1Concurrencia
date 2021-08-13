using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace prueba1Concurrencia
{
    class Stock
    {
        public DateTime Date { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }
    }
    class Program
    {
        private static  readonly HttpClient client = new HttpClient();
        private static object lockO = new object();
        static async Task Main(string[] args)
        {
            
            var msft = GetInfo("msft.us");
            var amzn = GetInfo("amzn.us");
            var fb = GetInfo("fb.us");
            var nflx = GetInfo("nflx.us");
            var aapl = GetInfo("aapl.us");


            var tasks = new List<Task>{ msft, amzn, fb, nflx, aapl };

            while (tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);

                if (task == msft)
                {
                    prom(msft.Result);
                }else if (task == aapl)
                {
                    prom(aapl.Result);
                }
                else if (task == amzn)
                {
                    prom(amzn.Result);
                }
                else if (task == fb)
                {
                    prom(fb.Result);
                }
                else
                {
                    prom(nflx.Result);
                }

                tasks.Remove(task);
            }

        }


        static void prom(List<Stock> result)
        {
            var proms = new ConcurrentBag<decimal>();
            
            var years = result.Select(x => x.Date.Year).Distinct();

            Parallel.ForEach(years, value =>
            {
                lock (lockO)
                {
                    var cont = result.Select(x => x.Date.Year).Where(x => x == value).Count();
                    var getClose = from x in result
                    where x.Date.Year == value
                    select x.Close;
                    proms.Add(getClose.Sum()/cont);
                }
                
            });
            foreach (var item in years)
            {
                Console.WriteLine(item);
            }

            var path = @"C:/Users/willi/OneDrive/Desktop/Concurrencia/jeje.Json";
            

            StreamWriter writer = File.AppendText(path);
            Parallel.ForEach(proms, val =>
            {
                if (!File.Exists(path))
                {
                    var newJson = JsonConvert.SerializeObject(val);
                    File.WriteAllText(path, newJson);
                    Console.WriteLine(val);
                }
                else
                {
                    writer.WriteLine($"\n{val}");
                    
                    Console.WriteLine(val);
                }
                
            });
            writer.Close();
        }
         static  async  Task<List<Stock>> GetInfo(string x)
        {
            var uri = await client.GetStreamAsync($"https://stooq.com/q/d/l/?s={x}&i=d");

            using (var reader = new StreamReader(uri))
            {
                using (var csvReader = new CsvReader(reader, new
                    CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        MissingFieldFound = null
                    }))
                {
                    return csvReader.GetRecords<Stock>().ToList();
                }
            }
        }
    }
}
