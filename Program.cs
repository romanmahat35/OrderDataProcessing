using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderDataPreparation
{
    class Program
    {
        static void Main(string[] args)
        {

            var outpoutfilelocation = ConfigurationManager.AppSettings["OutPutFilePath"];
            try
            {
                Log.Trace("プログラムの開始...");
                DataAcess da = new DataAcess();
                var inputfile = da.DownloadOrderData();
                if (inputfile.Length > 0)
                {
                    var inputSeq = da.GetSequence('B');
                    var output = da.GiveSequenceToOrderData(inputfile, ref inputSeq);
                    da.DumpOrderData(output, outpoutfilelocation);
                    da.InsertIntoTable();
                    Log.Trace("プログラムを完了しました...");
                }
            }
            catch (Exception ex)
            {

                Log.Error($"{ex}");
            }
            finally
            {
                Console.WriteLine("任意のキーを押して続行します。");
                Log.printline();
                Console.ReadKey();

            }


        }
            
              
               


            
    }
}
