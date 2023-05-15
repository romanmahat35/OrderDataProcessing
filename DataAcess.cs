using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace OrderDataPreparation
{
    class DataAcess
    {


        public class OrderData
        {
            public int COOPKBN;
            public int SISYOCD;
            public string CRSCD;
            public int CRSJUNI;
            public int HANCD;
            public string HANNMK;
            public string HANNMN;
            public int KUMICD;
            public string KNAMEK;
            public string KNAMEN;
            public string POSTNO;
            public string JYUSYON;
            public string TELNO;
            public int CHUNO;
            public int SURYO;
            public int BOOKNO;
            public int JUCHUKAI;

            // product data

        }

        public class OrderDataOut
        {
            public int COOPKBN;
            public int SISYOCD;
            public string CRSCD;
            public int CRSJUNI;
            public int HANCD;
            public string HANNMK;
            public string HANNMN;
            public int KUMICD;
            public string KNAMEK;
            public string KNAMEN;
            public string POSTNO;
            public string JYUSYON;
            public string TELNO;
            public OrderDataOutBody[] Bodies;
        }

        public class OrderDataOutBody
        {
            public int SAIBAN;
            public int CHUNO;
            public int SURYO;
        }

        public  OrderDataOut OrderDataOutFromIn(OrderData input)
        {
            return new OrderDataOut
            {
                COOPKBN = input.COOPKBN,
                SISYOCD = input.SISYOCD,
                CRSCD = input.CRSCD,
                CRSJUNI = input.CRSJUNI,
                HANCD = input.HANCD,
                HANNMK = input.HANNMK,
                HANNMN = input.HANNMN,
                KUMICD = input.KUMICD,
                KNAMEK = input.KNAMEK,
                KNAMEN = input.KNAMEN,
                POSTNO = input.POSTNO,
                JYUSYON = input.JYUSYON,
                TELNO = input.TELNO,
            };
        }

        public class Sequence
        {
            public int Position;
            public int Start;
            public int End;

            public int FetchCursorIncrement()
            {
                Position += 1;
                if (Position > End)
                    Position = Start;
                return Position;
            }
        }

        public  OrderDataOut[] GiveSequenceToOrderData(OrderData[] inputOrder, ref Sequence inputSeq)
        {

            Log.Trace("起動したファイルデータにシーケンスを付与する。");
            var outputOrder = new List<OrderDataOut>(inputOrder.Length);
            var outputBodies = new OrderDataOutBody[4]
            {
                            new OrderDataOutBody(),
                            new OrderDataOutBody(),
                            new OrderDataOutBody(),
                            new OrderDataOutBody()
            };

            outputOrder.Add(OrderDataOutFromIn(inputOrder[0]));
            outputBodies[0].SAIBAN = inputSeq.FetchCursorIncrement();
            outputBodies[0].CHUNO = inputOrder[0].CHUNO;
            outputBodies[0].SURYO = inputOrder[0].SURYO;
            int outputBodyLen = 1;

            for (var inputIndex = 1; inputIndex < inputOrder.Length; ++inputIndex)
            {
                var lastIndex = outputOrder.Count - 1;

                if (inputOrder[inputIndex].COOPKBN == outputOrder[lastIndex].COOPKBN &&
                    inputOrder[inputIndex].SISYOCD == outputOrder[lastIndex].SISYOCD &&
                    inputOrder[inputIndex].KUMICD == outputOrder[lastIndex].KUMICD &&
                    outputBodyLen < 4)
                {
                    outputBodies[outputBodyLen].SAIBAN = inputSeq.FetchCursorIncrement();
                    outputBodies[outputBodyLen].CHUNO = inputOrder[inputIndex].CHUNO;
                    outputBodies[outputBodyLen].SURYO = inputOrder[inputIndex].SURYO;
                    outputBodyLen += 1;
                }
                else
                {
                    outputOrder[lastIndex].Bodies = new OrderDataOutBody[outputBodyLen];
                    for (int i = 0; i < outputBodyLen; ++i)
                    {
                        outputOrder[lastIndex].Bodies[i] = outputBodies[i];
                    }

                    outputOrder.Add(OrderDataOutFromIn(inputOrder[inputIndex]));
                    outputBodies[0].SAIBAN = inputSeq.FetchCursorIncrement();
                    outputBodies[0].CHUNO = inputOrder[inputIndex].CHUNO;
                    outputBodies[0].SURYO = inputOrder[inputIndex].SURYO;
                    outputBodyLen = 1;
                }
            }

            {
                var lastIndex = outputOrder.Count - 1;
                outputOrder[lastIndex].Bodies = new OrderDataOutBody[outputBodyLen];
                for (int i = 0; i < outputBodyLen; ++i)
                {
                    outputOrder[lastIndex].Bodies[i] = outputBodies[i];
                }
                Log.Trace("ファイルデータが終了したときのシーケンスを表示します");
            }

            return outputOrder.ToArray();
            
        }

        public Sequence GetSequence( char id)
        {
            var query = $"SELECT SAIBAN,SAIBANST,SAIBANED FROM test WHERE SAIBANKBN='{id}'";

            using (var cmd = new OracleCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    Sequence seq = new Sequence
                    {
                        Position = reader.GetInt32(0),
                        Start = reader.GetInt32(1),
                        End = reader.GetInt32(2)
                    };

                    return seq;
                }
            }

        }

            public  void DumpOrderData(OrderDataOut[] orderDataOut, string filePath)
        {

            Log.Trace("ファイルエクスポート開始");
            MemoryStream stream = new MemoryStream(1 * 1024 * 1024);
            var ShiftJISCP = 932;
            var encoder = Encoding.GetEncoding(ShiftJISCP);
            var terminator = encoder.GetBytes("\r\n");
            var hanPadding = encoder.GetBytes("                                                                                ");
            var zenPadding = encoder.GetBytes("　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　");

            foreach (var o in orderDataOut)
            {
                DumpOrderDataRow(ref stream, o, encoder, hanPadding, zenPadding, terminator);
            }

            {
                var lastRow = encoder.GetBytes($"ﾔﾂ2{DateTime.Now.ToString("yyMMdd")}{orderDataOut.Length:00000}");
                stream.Write(lastRow, 0, lastRow.Length);
            }

            using (var sink = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                var encoded = stream.GetBuffer();
                sink.Write(encoded, 0, encoded.Length);
            }
        }

        public  void DumpOrderDataRow(ref MemoryStream stream, OrderDataOut data, Encoding encoder, byte[] hanPadding, byte[] zenPadding, byte[] terminator)
        {
            var bytes = encoder.GetBytes($"ﾔﾂ1{data.COOPKBN}{data.SISYOCD:000}{data.CRSCD}{data.CRSJUNI:000}{data.HANCD:0000000}");
            stream.Write(bytes, 0, bytes.Length);

            bytes = encoder.GetBytes(data.HANNMK);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(hanPadding, 0, 16 - bytes.Length);

            bytes = encoder.GetBytes(data.HANNMN);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(zenPadding, 0, 16 - bytes.Length);

            bytes = encoder.GetBytes($"{data.KUMICD:00000000}");
            stream.Write(bytes, 0, bytes.Length);

            bytes = encoder.GetBytes(data.KNAMEK);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(hanPadding, 0, 16 - bytes.Length);

            bytes = encoder.GetBytes(data.KNAMEN);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(zenPadding, 0, 16 - bytes.Length);

            bytes = encoder.GetBytes(data.POSTNO);
            stream.Write(bytes, 0, bytes.Length);

            bytes = encoder.GetBytes(data.JYUSYON);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(zenPadding, 0, 80 - bytes.Length);

            bytes = encoder.GetBytes(data.TELNO);
            stream.Write(bytes, 0, bytes.Length);

            foreach (var body in data.Bodies)
            {
                bytes = encoder.GetBytes($"{body.SAIBAN:000000}");
                stream.Write(bytes, 0, bytes.Length);
                bytes = encoder.GetBytes($"{body.CHUNO:000000}");
                stream.Write(bytes, 0, bytes.Length);
                bytes = encoder.GetBytes($"{body.SURYO:00}");
                stream.Write(bytes, 0, bytes.Length);
            }

            for (int i = data.Bodies.Length; i < 4; ++i)
            {
                stream.Write(hanPadding, 0, 14);
            }
            stream.Write(hanPadding, 0, 7);
            stream.Write(terminator, 0, terminator.Length);
           
        }

        public OrderData[] DownloadOrderData()
        {
            var query = @"SELECT COOPKBN, SISYOCD, CRSCD, CRSJUNI,
                        HANCD, HANNMK, HANNMN, KUMICD, KNAMEK, KNAMEN,
                        POSTNO, JYUSYON, TELNO, CHUNO, SURYO, BOOKNO, JUCHUKAI
                        FROM test ORDER BY COOPKBN,SISYOCD,KUMICD";

            using (var cmd = new OracleCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    List<OrderData> orderData = new List<OrderData>((int)reader.RowSize);

                    while (reader.Read())
                    {
                        OrderData data = new OrderData
                        {
                            COOPKBN = reader.GetInt32(0),
                            SISYOCD = reader.GetInt32(1),
                            CRSCD = reader.GetString(2),
                            CRSJUNI = reader.GetInt32(3),
                            HANCD = reader.GetInt32(4),
                            HANNMK = reader.GetString(5),
                            HANNMN = reader.GetString(6),
                            KUMICD = reader.GetInt32(7),
                            KNAMEK = reader.GetString(8),
                            KNAMEN = reader.GetString(9),
                            POSTNO = reader.GetString(10),
                            JYUSYON = reader.GetString(11),
                            TELNO = reader.IsDBNull(12) ? "" : reader.GetString(12),
                            CHUNO = reader.GetInt32(13),
                            SURYO = reader.GetInt32(14),
                            BOOKNO = reader.GetInt32(15),
                            JUCHUKAI = reader.GetInt32(16)
                        };

                        orderData.Add(data);
                    }

                    return orderData.ToArray();
                }
            }
        }

        private OracleConnection conn = null;
        
        public DataAcess()
        {
            var connectionstring = ConfigurationManager.ConnectionStrings["KUMISERVER"].ConnectionString;
            try
            {
                Log.Trace("データベース接続を開始しました。");

                
                conn = new OracleConnection(connectionstring);
                conn.Open();
                Log.Trace("データベースが接続されています。");
            }
            catch (Exception ex)
            {

                Log.Error($"{connectionstring} 存在しません。: {ex}");
            }
        }

        public void InsertIntoTable()
        {
            Log.Trace("テーブルへの挿入が始まりました。");
            var TAXKBN0  = int.Parse(ConfigurationManager.AppSettings["TAXKBN0"]);
            var TAXKBN1  = int.Parse(ConfigurationManager.AppSettings["TAXKBN1"]);
            var TAXRITU0 = int.Parse(ConfigurationManager.AppSettings["TAXRITU0"]);
            var TAXRITU1 = int.Parse(ConfigurationManager.AppSettings["TAXRITU1"]);
            var TAXRITU2 = int.Parse(ConfigurationManager.AppSettings["TAXRITU2"]);
            var KOSINCOOPKBN = int.Parse(ConfigurationManager.AppSettings["KOSINCOOPKBN"]);
            var KOSINSISYOCD = int.Parse(ConfigurationManager.AppSettings["KOSINSISYOCD"]);
            var KOSINID = int.Parse(ConfigurationManager.AppSettings["KOSINID"]);
            var DataLifeInDays = int.Parse(ConfigurationManager.AppSettings["DataLifeInDays"]);

            var insertsql = "SPR_INSERTORDERPRODUCTDATA3";
            OracleCommand cmd = conn.CreateCommand();
            cmd.CommandText = insertsql;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("v_TAXKBN0", OracleDbType.Int32).Value = TAXKBN0;
            cmd.Parameters.Add("v_TAXKBN1", OracleDbType.Int32).Value = TAXKBN1;
            cmd.Parameters.Add("v_TAXRITU0", OracleDbType.Int32).Value = TAXRITU0;
            cmd.Parameters.Add("v_TAXRITU1", OracleDbType.Int32).Value = TAXRITU1;
            cmd.Parameters.Add("v_TAXRITU2", OracleDbType.Int32).Value = TAXRITU2;
            cmd.Parameters.Add("v_KOSINCOOPKBN", OracleDbType.Int32).Value = KOSINCOOPKBN;
            cmd.Parameters.Add("v_KOSINSISYOCD", OracleDbType.Int32).Value = KOSINSISYOCD;
            cmd.Parameters.Add("v_KOSINID", OracleDbType.Int32).Value = KOSINID;
            cmd.Parameters.Add("V_DataLifeInDays", OracleDbType.Int32).Value = DataLifeInDays;
            cmd.ExecuteNonQuery();
            Log.Trace("テーブルへの挿入が終了しました。");


        }

    }
}
