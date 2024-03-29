﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class ClientObject
    {
        public int ID { get; private set; }
        private TcpClient tcpClient;
        private BinaryReader reader;
        private BinaryWriter writer;

        public ClientObject(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            this.reader = new BinaryReader(tcpClient.GetStream());
            this.writer = new BinaryWriter(tcpClient.GetStream());

            new Thread(new ThreadStart(Process)).Start();
        }

        public void Process()
        {
            while (true)
            {
                try
                {
                    int operation = reader.ReadInt32();

                    switch (operation)
                    {
                        case 0:
                            {
                                string login = reader.ReadString();
                                string password = reader.ReadString();

                                Logined(login, password);

                                break;
                            }
                        case 1:
                            {
                                string table = reader.ReadString();

                                SelectOperation(table);

                                break;
                            }
                        case 2:
                            {
                                string table = reader.ReadString();
                                int length = reader.ReadInt32();
                                string[] args = new string[length];

                                for (int i = 0; i < length; i++)
                                {
                                    args[i] = reader.ReadString();
                                }
                                AddOpertion(table, length, args);

                                break;
                            }
                        case 3:
                            {
                                string table = reader.ReadString();
                                DataSet dst = new DataSet();
                                SqlDataAdapter adapter = new SqlDataAdapter($"select * from {table}", Program.CONNECTION_STRING);

                                SearchContexInfo(table, dst, adapter);

                                break;
                            }
                        case 4:
                            {
                                string table = reader.ReadString();
                                int length = reader.ReadInt32();
                                string[] args = new string[length];
                                string text = reader.ReadString();

                                for (int i = 0; i < length; i++)
                                {
                                    args[i] = reader.ReadString();
                                }

                                Console.WriteLine(args[0]);
                                Console.WriteLine(args[1]);

                                DataSet dst = new DataSet();
                                SearchInfo(table, args, text, dst);

                                break;
                            }
                        case 5:
                            {
                                string table = reader.ReadString();
                                int length = reader.ReadInt32();
                                string[] args = new string[length];

                                for (int i = 0; i < length; i++)
                                {
                                    args[i] = reader.ReadString();
                                }

                                AverageScore(table, args);

                                break;
                            }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"{ID}: {ex.ToString()}");
                    Disconnect();
                    break;
                }
            }
        }

        private void AverageScore(string table, string[] args)
        {
            DataSet dst = new DataSet();
            SqlDataAdapter adapter = new SqlDataAdapter($"select СрБалл from {table} where КодСтуд = '{args[0]}' " +
            $"and КодДисц = '{args[1]}'", Program.CONNECTION_STRING);
            adapter.Fill(dst);

            writer.Write(dst.GetXml());
        }

        private void SearchContexInfo(string table, DataSet dst, SqlDataAdapter adapter)
        {
            if (table == "Факультеты")
                adapter.Fill(dst, "f");
            else if (table == "Группы")
                adapter.Fill(dst, "g");
            else if (table == "Преподаватели")
                adapter.Fill(dst, "t");
            else if (table == "Студенты")
                adapter.Fill(dst, "st");
            else if (table == "Дисциплины")
                adapter.Fill(dst, "disc");
            else
                adapter.Fill(dst, "fc");

            writer.Write(dst.GetXml());
        }

        private void SearchInfo(string table, string[] args, string text, DataSet dst)
        {
            if (table == "[St_AcPerformance]" && text == "StudPerform")
            {
                SqlDataAdapter adapter = new SqlDataAdapter($"select * from {table} where Семестр = '{args[0]}' " +
                    $"and КодСтуд = '{args[1]}'", Program.CONNECTION_STRING);
                adapter.Fill(dst);
            }
            else if (table == "Report" && text == "Report")
            {
                SqlDataAdapter adapter = new SqlDataAdapter($"select * from {table} where Семестр = '{args[0]}' " +
                    $"and Группа = '{args[1]}'", Program.CONNECTION_STRING);
                adapter.Fill(dst);
            }
            else
            {
                SqlDataAdapter adapter = new SqlDataAdapter($"select * from {table} where Семестр = '{args[0]}' and КодГруппы = '{args[1]}' " +
                    $"and КодДисц = '{args[2]}'", Program.CONNECTION_STRING);
                adapter.Fill(dst);
            }

            writer.Write(dst.GetXml());
        }

        private void AddOpertion(string table, int length, string[] args)
        {
            string command = $"insert into {table} values('{args[0]}'";

            for (int i = 1; i < length; i++)
            {
                command += $",'{args[i]}'";
            }
            command += ')';

            SqlCommand sqlCommand = new SqlCommand(command, new SqlConnection(Program.CONNECTION_STRING));

            bool result = false;

            try
            {
                sqlCommand.Connection.Open();
                sqlCommand.ExecuteNonQuery();
                sqlCommand.Connection.Close();
                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ID}:" + ex.ToString());
                result = false;
            }

            writer.Write(result);
        }

        private void SelectOperation(string table)
        {
            DataSet dst = new DataSet();
            SqlDataAdapter adapter = new SqlDataAdapter($"select * from {table}", Program.CONNECTION_STRING);
            adapter.Fill(dst);

            writer.Write(dst.GetXml());
        }

        private void Logined(string login, string password)
        {
            DataSet dst = new DataSet();
            SqlDataAdapter adapter = new SqlDataAdapter($"select * from Users  where Login='{login}' and Password='{password}'", Program.CONNECTION_STRING);
            adapter.Fill(dst);

            bool result = dst.Tables[0].Rows.Count > 0;
            writer.Write(result);

            if (result)
            {
                ID = int.Parse(dst.Tables[0].Rows[0][0].ToString());
                Console.WriteLine($"{ID}: Connected");
            }
        }

        private void Disconnect()
        {
            tcpClient.Close();
            reader.Close();
            writer.Close();
            Console.WriteLine($"{ID}: Disconneted");
        }
    }
}
