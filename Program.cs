using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Timers;
using System.Threading;


namespace ConsoleApp13
{
    class Program
    {
        static void Main(string[] args)
        {
            chat vas = new chat();
            vas.proc();
        }
    }
    class chat
    {
        string ExceptionDescription;
        bool Run = true;//переменная запрещяющия запуску более 1 принятия порта в счетчике
        bool FilesSave = false;
        string SaveWay;
        private SerialPort port = new SerialPort();
        bool SendOpp = true;
        string ReciveFileName;

        public void timer_Elapsed(object o, ElapsedEventArgs e)
        {
            if (Run)
            {
                Run = false;//запрет запуска принятия порта
                ErrorPacket res = new ErrorPacket();
                var packet = new Packet();
                if (port.IsOpen)
                {
                    if (port.BytesToRead > 0)
                    {
                        res = packet.ReciveData(port, 100);
                        int typeFile = packet.DataTypeSearch(res.Values);//поиск типа данных с пакете
                        if (typeFile == 4)//если тип пакетов 4(file)
                        {
                            FilesSave = true;
                            Run = true;
                            SaveFile(res, typeFile);//сохранение файла
                        }
                        if (typeFile == 6)
                        {
                            string code = packet.ConvertPaketsInString(res.Values);
                            string[] arr = code.Split('|');
                            if (code == "Command.SendFile")
                            {
                                SendOpp = false;
                            }
                            if (arr[0] == "Command.SendFileName")
                            {
                                if (ReciveFileName == null)
                                {
                                    ReciveFileName = arr[arr.Length - 1];
                                }
                                else
                                {
                                    SendMessage($"SEND/[Фаил {arr[arr.Length - 1]} не может быть принят пока принимающий клиент обрабатывает предыдущий фаил]", port, false, TypesContent.Message);
                                    Console.WriteLine("Фаил не может быть принят пока вы обрабатываете предыдущий фаил");
                                }
                            }
                        }
                        else
                        {
                            if (res.ErrorCode == 0 && typeFile == 3)
                            {
                                string mes = packet.ConvertPaketsInString(res.Values);//перевод пакета в строку
                                Console.WriteLine($"Принятое сообщение: {mes}");
                            }
                        }

                    }
                }
                Run = true;//разрешение запуска принятия порта
            }
        }
        private System.Timers.Timer CheckTime = new System.Timers.Timer();
        public void proc()
        {
            ErrorPacket res = new ErrorPacket();
            var packet = new Packet();
            CheckTime.Start();
            CheckTime.Interval = 100;
            CheckTime.Elapsed += timer_Elapsed;
            bool run = true;
            string vvod;
            bool ok;
            Console.WriteLine("Для вывода списка комманд введите [Help/]");
            while (run)
            {
                ok = false;
                Console.Write("-->");
                vvod = Console.ReadLine();
                string sub = SubString(vvod, "/");
                try
                {
                    sub = sub.ToUpper();
                }
                catch { }
                if (sub == "SERIALPORT" && !FilesSave)
                {
                    try
                    {
                        if (!port.IsOpen)
                        {
                            port = PreparateSerialPort(vvod);
                            Console.WriteLine($"{port.PortName} {port.BaudRate} {port.Parity} {port.DataBits}");
                            Console.WriteLine("Открытие порта...");
                            port.Open();
                            Console.WriteLine("Порт открыт");
                        }
                        else
                        {
                            Console.WriteLine("Порт уже был открыт");
                        }

                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message); // вывод ошибки 
                    }
                    ok = true;
                }
                if (sub == "PORTCLOSE" && !FilesSave)
                {
                    if (port.IsOpen)
                    {
                        Console.WriteLine($"Порт {port.PortName} закрыт");
                        port.Close();
                    }
                    else
                    {
                        Console.WriteLine("Порт не был открыт");
                    }
                    ok = true;
                }
                if (sub == "LISTPORTS" && !FilesSave)
                {
                    string[] ports = SerialPort.GetPortNames();
                    for (int i = 0; i < ports.Length; i++)
                    {
                        Console.WriteLine(ports[i]);
                    }
                    ok = true;
                }
                if (sub == "SEND" && !FilesSave)
                {
                    if (port.IsOpen)
                        SendMessage(vvod, port, true, TypesContent.Message);
                    else
                        Console.WriteLine("Подключитесь в начале к порту");

                    ok = true;
                }
                if (sub == "HELP" && !FilesSave)
                {
                    Console.WriteLine("SerialPort/ [Имя порта] [BaudRate] [Parity] [DataBits] -->Открытие порта");
                    Console.WriteLine("Send/[Сообщение]       -->Отправка сообщения");
                    Console.WriteLine("PortClose/             -->Закрытие порта");
                    Console.WriteLine("SendFile/[путь к файлу]-->Отправка файла");
                    Console.WriteLine("CheckPort/             -->Просмотр параметров порта");
                    Console.WriteLine("ListPorts/              -->просмотреть список портов");
                    ok = true;
                }
                if (sub == "CHECKPORT" && !FilesSave)
                {
                    Console.WriteLine($"{port.PortName} {port.BaudRate} {port.Parity} {port.DataBits}");
                    if (port.IsOpen)
                    {
                        Console.WriteLine("Порт открыт");
                    }
                    else
                    {
                        Console.WriteLine("Порт закрыт");
                    }
                    ok = true;
                }
                if (sub == "СLEARRECIVESTATUS")
                {
                    SendOpp = true;
                    Console.WriteLine("Теперь вы снова можете отправлять файлы");
                    ok = true;
                }
                if (sub == "TEST")
                {
                    ErrorPacket pak = new ErrorPacket();
                    pak = pak.Packet.PreparationData("C:\\база\\image.png",TypesContent.File,1,2);
                    pak = pak.Packet.SaveDataInFile("C:\\база\\МачкаПасла.png", pak.Values);
                    ok = true;
                    Console.WriteLine("МачкаПасла.png");
                }
                if (sub == "SENDFILE" && !FilesSave)
                {
                    if (port.IsOpen)
                    {
                        if (SendOpp)
                        {
                            SendFile(vvod, port);
                        }
                        else
                        {
                            Console.WriteLine("Дождитесь окончания принятия отправленного вам файла");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Подключитесь в начале к порту");
                    }
                    ok = true;
                }

                if (FilesSave)
                {
                    SaveWay = vvod;
                    ok = true;
                }
                if (!ok)
                {
                    Console.WriteLine("Комманда не обнаружена");
                }
            }
            Console.ReadKey();
        }
        /// <summary>
        /// Поиск первого слова
        /// </summary>
        /// <param name="text">Обозреваемый текст</param>
        /// <param name="item">Элемент, до которого будет определяться первое слова</param>
        /// <param name="startPoint"></param>
        /// <returns>возвращает строку или в случае ошибки возвращает "ничего"</returns>
        private string SubString(string text, string item)
        {
            try
            {
                int subStringSize = text.IndexOf(item);
                string s = text.Substring(0, subStringSize);
                return s;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Задание параметров порта
        /// </summary>
        /// <param name="vvod">Расшифровываемое сообщение</param>
        /// <returns>порт с параметрами</returns>
        private SerialPort PreparateSerialPort(string vvod)
        {
            SerialPort ret = new SerialPort();
            try
            {
                ret = new SerialPort("COM1", 9000, Parity.None, dataBits: 8);//задание базовых параметров
                string[] arr = vvod.Split(' ');//расшифровка параметров комманды
                if (arr.Length > 1)
                {
                    ret.PortName = arr[1];//ввод имени порта, если в комманде имеется имя
                    if (arr.Length > 2)
                    {
                        ret.BaudRate = Convert.ToInt32(arr[2]);//ввод параметра BaudRate
                        if (arr.Length > 3) //Ввод параметра parity если он имеется в комманде
                        {
                            if (arr[3] == "0" || arr[3] == "None")
                                ret.Parity = Parity.None;
                            if (arr[3] == "1" || arr[3] == "Odd")
                                ret.Parity = Parity.Odd;
                            if (arr[3] == "2" || arr[3] == "Even")
                                ret.Parity = Parity.Even;
                            if (arr[3] == "3" || arr[3] == "Mark")
                                ret.Parity = Parity.Mark;
                            if (arr[3] == "4" || arr[3] == "Space")
                                ret.Parity = Parity.Space;
                        }
                        if (arr.Length > 4)
                        {
                            ret.DataBits = Convert.ToInt32(arr[4]);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);//вывод ошибки, если некоторые данные некорректны
            }
            return ret;
        }
        /// <summary>
        /// Отправка сообщения
        /// </summary>
        /// <param name="message">Отправляемое сообщение</param>
        /// <param name="port">порт на который сообщение будет отправлено</param>
        /// <param name="debug">сообщать ли о том что сообщение отправлено</param>
        private void SendMessage(string message, SerialPort port, bool debug, TypesContent type)
        {
            string[] arr = message.Split('/');//отделение сообщения от комманды
            var packet = new Packet();
            var res = packet.PreparationData(arr[1], type, 3, 4);//конвертация строки в пакеты
            if (res.ErrorCode == 0)
            {
                res = packet.SendData(port, 10000, res);
                if (debug)
                {
                    if (res.ErrorCode == 0)
                    {
                        Console.WriteLine("Сообщение отправлено");
                    }
                    else
                    {
                        Console.WriteLine("Сообщение не отправлено");
                    }
                }
            }
            else
            {
                Console.WriteLine(res.ErrorDescription);
            }
        }
        /// <summary>
        /// Отправка файла
        /// </summary>
        /// <param name="message">расшифровываемое сообщение</param>
        /// <param name="port">Порт для отправки</param>
        private void SendFile(string message, SerialPort port)
        {
            ErrorPacket res = new ErrorPacket();
            try
            {
                string[] arr;
                arr = message.Split('/');//отделение комманды от пути файла
                string way = arr[1];
                var packet = new Packet();
                if (File.Exists(way))//проверка существования файла
                {
                    arr = message.Split('.');
                    SendMessage($"SEND/[Вам начат отправлятся фаил типа {arr[arr.Length - 1]}]", port, false, TypesContent.Message);
                    arr = way.Split('\\');
                    SendMessage($"SEND/Command.SendFileName|{arr[arr.Length - 1]}", port, false, TypesContent.Command);
                    SendMessage("SEND/Command.SendFile", port, false, TypesContent.Command);
                    res = packet.PreparationData(way, TypesContent.File, 3, 4);//конвертация файла в пакеты
                    if (res.ErrorCode == 0)
                    {
                        res = packet.SendData(port, 100000000, res);
                        if (res.ErrorCode == 0)
                        {
                            Console.WriteLine("Файл отправлен");
                        }
                        else
                        {
                            Console.WriteLine("Файл не отправлен");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Фаил не был найден");
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
        /// <summary>
        /// Сохранение полученного файла
        /// </summary>
        /// <param name="packet">Массив пакетов</param>
        /// <param name="type">Тип данных</param>
        private void SaveFile(ErrorPacket packet, int type)
        {
            bool first = true;
            bool end = true;
            while (end)
            {
                SaveWay = null;
                FilesSave = true;
                if (first)
                {
                    Console.WriteLine($"Вы приняли фаил {ReciveFileName}, сохранить\n" +
                        "1.Да\n" +
                        "Иное.нет\n");
                    while (SaveWay == null)//ожидание ввода параметра подтверждения
                    { }
                }
                else
                {
                    SaveWay = "1";
                }
                try
                {
                    if (SaveWay == "1")
                    {
                        first = false;
                        SaveWay = null;
                        Console.WriteLine("Введите имя файла и путь");
                        while (SaveWay == null) { }//ожидание ввода пути сохранения
                        SaveWay = SaveWay.Replace("\\\\", "\\");
                        string[] filedirectory = SaveWay.Split('\\');//определение пути для сохраняемого файла
                        string fileDir = "";
                        for (int i = 0; i < filedirectory.Length - 1; i++)
                        {
                            fileDir += filedirectory[i];
                            fileDir += "\\";
                        }
                        Directory.CreateDirectory(fileDir);//создание дирректории для файла
                        packet = packet.Packet.SaveDataInFile(SaveWay, packet.Values);
                        if (File.Exists(SaveWay))
                        {
                            Console.WriteLine("Фаил сохранен");
                            ExceptionDescription = null;
                            end = false;
                            SendMessage($"SEND/[Фаил {ReciveFileName} принят успешно]", port, false, TypesContent.Message);
                        }
                        else
                        {
                            Console.WriteLine("Фаил не сохранен");
                            ExceptionDescription = packet.ErrorDescription;
                            Console.WriteLine(ExceptionDescription);
                            Console.WriteLine("Повторить попытку [1)да] [иное)нет] ");
                            SaveWay = "";
                            while (SaveWay == "") { }
                            if (SaveWay != "1") { end = false; SendMessage($"SEND/[Фаил {ReciveFileName} не был принят]", port, false, TypesContent.Message); }
                        }
                    }
                    else
                    {
                        SendMessage($"SEND/[Фаил {ReciveFileName} не был принят]", port, false, TypesContent.Message);
                        end = false;
                    }
                    SendOpp = true;
                }
                    catch (Exception err) { }
                {
                    Console.WriteLine(err.Message);
                    Console.WriteLine("Повторить попытку [1)да] [иное)нет] ");
                    SaveWay = "";
                    while (SaveWay == "") { }
                    if (SaveWay != "1") { end = false; SendMessage($"SEND/[Фаил {ReciveFileName} не был принят]", port, false, TypesContent.Message); }
                }
        }
        ReciveFileName = null;
            FilesSave = false;
        }
    }
}