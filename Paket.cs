using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

namespace ConsoleApp13
{
    public enum TypesContent {CRS=2, Message=3, File = 4, Error =5, Command=6}
    public class ErrorPacket
    {
        public int ErrorCode;
        public string ErrorDescription;
        public Packet Packet;
    }

    public class Packet
    {
        public byte[,] DataRes; //индекс 1-номер пакета, индекс 2-номер байта
        private string Name; //2 символа 
        private int PocCount; //4 байта
        private int PocNum; //4 байта
        private ushort LengthData;//2 байта
        private int DataType; //4 байта
        private int SenderName; //4 байта
        private int ReciverName; //4 байта
        private byte[] Data;//до 600 байт
        private ushort CRS; //2 байта
        private byte[] paket;
        private byte[] CRSpaket;

        /// <summary>
        /// Определение количества пакетов в массиве пакетов
        /// </summary>
        /// <param name="DataRes">Массив пакетов</param>
        /// <returns>Количество пакетов</returns>
        private int PocNumDataSearch(byte[,] DataRes)
        {
            byte[] fourArr = new byte[4];
            for (int s = 0; s < 4; s++) { fourArr[s] = DataRes[0, s + 6]; }
            PocNum = BitConverter.ToInt32(fourArr, 0);
            return PocNum;
        }

        /// <summary>
        /// Определение размера данных в пакете
        /// </summary>
        /// <param name="DataRes">Массив пакетов</param>
        /// <param name="num">Номер пакета</param>
        /// <returns>Объем данных в пакете</returns>
        private ushort LengthDataSearch(byte[,] DataRes, int num)//поиск длинны даты---------------------------------
        {
            byte[] twoArr = new byte[2];
            for (int j = 0; j < 2; j++) { twoArr[j] = DataRes[num, j + 10]; }
            LengthData = BitConverter.ToUInt16(twoArr, 0);
            return LengthData;
        }
        /// <summary>
        /// Подсчет контрольной суммы
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="pocCount">Номер пакета</param>
        /// <param name="pocNum">Количество пакетов</param>
        /// <param name="lengthData">Объем данных</param>
        /// <param name="dataType">Тип данных</param>
        /// <param name="Data">Данные</param>
        /// <returns>Сумма всех элементов указанных массивов</returns>
        private ushort SchetCRS(byte[] name, byte[] pocCount, byte[] pocNum, byte[] lengthData,byte[] senderName,byte[] reciverName, byte[] dataType, byte[] Data)//Функция для подсчета контрольной суммы--------------------------------------
        {
            ushort CRS = 0;
            CRS += CalcControlSumm(name);
            CRS += CalcControlSumm(pocCount);
            CRS += CalcControlSumm(pocNum);
            CRS += CalcControlSumm(lengthData);
            CRS += CalcControlSumm(dataType);
            CRS += CalcControlSumm(senderName);
            CRS += CalcControlSumm(reciverName);
            CRS += CalcControlSumm(Data);           
            return CRS;
        }

        private ushort CalcControlSumm(byte[] crs)
        {
            ushort CRS=0;
            for (int i = 0; i < crs.Length; i++)
            { CRS += crs[i]; }
            return CRS;
        }
        /// <summary>
        /// Сборка данных в пакет
        /// </summary>
        /// <param name="Name">Имя пакета</param>
        /// <param name="PocCount">Номер пакета</param>
        /// <param name="PocNum">Количество пакетов</param>
        /// <param name="LengthData">Объем данных</param>
        /// <param name="dataType">Тип данных</param>
        /// <param name="Data">Данные</param>
        private void ConvertDataInPaket(string Name, int PocCount, int PocNum, ushort LengthData, int dataType, int senderName, int reciverName, byte[] Data)//конвертация данных в массив байтов------------------------------------------
        {
            ushort crs;
            byte[] arrName = Encoding.UTF8.GetBytes(Name);
            byte[] arrPocCount = BitConverter.GetBytes(PocCount);
            byte[] arrPocNum = BitConverter.GetBytes(PocNum);
            byte[] arrLenghtData = BitConverter.GetBytes(LengthData);
            byte[] arrDataType = BitConverter.GetBytes(dataType);
            byte[] arrSenderName = BitConverter.GetBytes(senderName);
            byte[] arrReciverName = BitConverter.GetBytes(reciverName);
            crs = SchetCRS(arrName, arrPocCount, arrPocNum, arrLenghtData, arrDataType, arrSenderName, arrReciverName, Data);
            byte[] arrCRS = BitConverter.GetBytes(crs);
            paket = new byte[arrName.Length + arrPocCount.Length + arrPocNum.Length + arrLenghtData.Length + arrDataType.Length + arrCRS.Length + Data.Length+ arrSenderName.Length+arrReciverName.Length];
            arrName.CopyTo(paket, 0);
            arrPocCount.CopyTo(paket, 2);
            arrPocNum.CopyTo(paket, 6);
            arrLenghtData.CopyTo(paket, 10);
            arrDataType.CopyTo(paket, 12);
            arrSenderName.CopyTo(paket,16);
            arrReciverName.CopyTo(paket,20);
            Data.CopyTo(paket, 24);
            arrCRS.CopyTo(paket, Data.Length + 24);
        }

        /// <summary>
        /// Подсчет контрольной суммы и сравнение её с той, что пришла в пакете
        /// </summary>
        /// <param name="Name">Имя пакета</param>
        /// <param name="pocCount">Номер пакета</param>
        /// <param name="pocNum">Количество пакетов</param>
        /// <param name="LengthData">Объем данных</param>
        /// <param name="DataType">Тип данных</param>
        /// <param name="Data">Данные</param>
        /// <param name="CRS">Контрольная сумма введенного пакета</param>
        /// <returns>true если контрольная сумма совпадает, и false если не совпадает</returns>
        private Boolean CheckCRS(string Name, int pocCount, int pocNum, ushort LengthData, int DataType, int senderName, int reciverName, byte[] Data, ushort CRS)
        {
            ushort checkcrs;
            byte[] arrName = Encoding.UTF8.GetBytes(Name);
            byte[] arrPocCount = BitConverter.GetBytes(pocCount);
            byte[] arrPocNum = BitConverter.GetBytes(pocNum);
            byte[] arrLenghtData = BitConverter.GetBytes(LengthData);
            byte[] arrDataType = BitConverter.GetBytes(DataType);
            byte[] arrSenderName = BitConverter.GetBytes(senderName);
            byte[] arrReciverName = BitConverter.GetBytes(reciverName);
            checkcrs = SchetCRS(arrName, arrPocCount, arrPocNum, arrLenghtData, arrDataType,arrSenderName,arrReciverName, Data);
            if (checkcrs != CRS)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        /// <summary>
        /// Разборка массива с пакетом на данные
        /// </summary>
        /// <param name="readbyte">Массив с пакетом</param>
        private void DeConvertDataOutPaket(byte[] readbyte)//деконвертация данных из массива байтов-------------------------------------------------,----------------------------------------
        {
            byte[] fourArr = new byte[4];
            byte[] twoArr = new byte[2];

            for (int i = 0; i < 2; i++) { twoArr[i] = readbyte[i]; }
            Name = Encoding.UTF8.GetString(twoArr);
            string pakName = Name;

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + 2]; }
            PocCount = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + 6]; }
            PocNum = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 2; i++) { twoArr[i] = readbyte[i + 10]; }
            LengthData = BitConverter.ToUInt16(twoArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + 12]; }
            DataType = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + 16]; }
            SenderName = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + 20]; }
            ReciverName = BitConverter.ToInt32(fourArr, 0);

            int Numdata = 24;
            Data = new byte[LengthData];
            for (int i = 0; i < LengthData; i++)
            {
                Data[i] = readbyte[Numdata];
                Numdata++;
            }

            for (int i = 0; i < 2; i++) { twoArr[i] = readbyte[Numdata + i]; }
            CRS = BitConverter.ToUInt16(twoArr, 0);
        }

        /// <summary>
        /// Сохранение указанного массива в массив пакетов
        /// </summary>
        /// <param name="schetPak">Номер пакета</param>
        /// <param name="lengthData">Объем данных</param>
        /// <param name="pakets">Массив из которого будут переписаны данные</param>
        private void SaveDataInMatrix(int schetPak, ushort lengthData, byte[] pakets)//сохранение массива на указанную строку------------------------------------------------------------------
        {
            for (int i = 0; i < lengthData + 26; i++)
            {
                DataRes[schetPak, i] = pakets[i];
            }
        }
        /// <summary>
        /// Сохранение массива пакетов в фаил
        /// </summary>
        /// <param name="fileName">Путь к файлу</param>
        /// <returns>Код ошибки и её описание</returns>
        public ErrorPacket SaveDataInFile(string fileName)//сохранение в фаил===============================
        {
            ErrorPacket err = new ErrorPacket();
            try
            {
                err.ErrorCode = 0;
                err.ErrorDescription = "";
                int AllDataL = 0;
                int PocN = PocNumDataSearch(DataRes);
                int schetByte = 0;
                for (int i = 0; i < PocN; i++)
                {
                    AllDataL += LengthDataSearch(DataRes, i);
                }
                byte[] saveArray = new byte[AllDataL];
                for (int i = 0; i < PocN; i++)
                {
                    int DataL = LengthDataSearch(DataRes, i);
                    for (int j = 24; j < DataL + 24; j++)
                    {
                        saveArray[schetByte] = DataRes[i, j];
                        schetByte++;
                    }
                }
                File.WriteAllBytes(fileName, saveArray);
                return err;
            }
            catch(Exception error)
            {
                err.ErrorCode = error.HResult;
                err.ErrorDescription = error.Message;
                return err;
            }
        }

        /// <summary>
        /// Конвертация строки или файла в массив пакетов
        /// </summary>
        /// <param name="stroka">Строка, конвертируемая в пакет</param>
        /// <param name="type">Тип данных</param>
        /// <returns>Код ошибки и её описание</returns>
        public ErrorPacket PreparationData(string stroka, TypesContent type, int SenderName, int ReciverName)//Подготовка строки к разрезке========================================
        {
            ErrorPacket pockets = new ErrorPacket();
            pockets.ErrorCode = 0;
            pockets.ErrorDescription = null;
            if (type != TypesContent.File)
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(stroka);
                pockets = PreparationData(byteArray, type, SenderName, ReciverName);
            }
            else
            {
                try
                {
                    byte[] byteArray = File.ReadAllBytes(stroka);
                    pockets = PreparationData(byteArray, type, SenderName, ReciverName);
                }
                catch (Exception err)
                {
                    pockets.ErrorCode = err.HResult;
                    pockets.ErrorDescription = err.Message;
                }
            }
            return pockets;
        }
        /// <summary>
        /// Конвертация массива байтов в пакет
        /// </summary>
        /// <param name="byteArray">Массив байтов, конвертируемый</param>
        /// <param name="type">Тип данных</param>
        /// <returns>Код ошибки и её описание</returns>
        public ErrorPacket PreparationData(byte[] byteArray, TypesContent type, int senName, int recName)//Разрезка массива байтов на пакеты=====================================================================================
        {
            SenderName = senName;
            ReciverName = recName;
            DataType = (int)type;
            ErrorPacket pockets = new ErrorPacket();
            try
            {
                pockets.ErrorCode = 0;
                pockets.ErrorDescription = null;
                Random rand = new Random();
                string nameRand = Convert.ToString(rand.Next(10, 99));//генерация имени пакетов

                int schetBytes = 0;
                int ostatok = 0;
                int schetPak = 0;
                if (byteArray.Length < 600)         //
                {                                   //
                    ostatok = 1;                    //
                }                                   // определение наличие пакета,, объем которого меньше 600
                if (byteArray.Length % 600 > 0)     //
                {                                   //
                    ostatok = 1;                    //
                }
                int pacKolvo = byteArray.Length / 600; //определение количества пакетов объемом в 600 байт
                PocNum = pacKolvo + ostatok;//подсчет количества пакетов кратных 600 и пакета не кратного 600
                DataRes = new byte[PocNum, 626];

                if (byteArray.Length > 600)//начало отправки пакетов с данными, объем которых = 600
                {
                    for (int schet = 0; schet < pacKolvo; schet++)
                    {
                        Name = nameRand;
                        PocCount = schetPak;

                        LengthData = 600;
                        Data = new byte[LengthData];
                        for (int i = 0; i < 600; i++)
                        {
                            Data[i] = byteArray[schetBytes];
                            schetBytes++;
                        }
                        ConvertDataInPaket(Name, PocCount, PocNum, LengthData, DataType, SenderName, ReciverName, Data);
                        SaveDataInMatrix(schetPak, LengthData, paket);
                        schetPak++;
                    }
                }
                if (ostatok == 1 || byteArray.Length < 600)//Отправка пакета объемом <600 если таковой имеется
                {
                    Name = nameRand;
                    PocCount = schetPak;
                    PocNum = pacKolvo + ostatok;
                    LengthData = Convert.ToUInt16(byteArray.Length % 600);
                    Data = new byte[LengthData];
                    for (int i = 0; i < byteArray.Length % 600; i++)
                    {
                        Data[i] = byteArray[schetBytes];
                        schetBytes++;
                    }
                    ConvertDataInPaket(Name, PocCount, PocNum, LengthData, DataType, SenderName, ReciverName, Data);//сборка пакета
                    SaveDataInMatrix(schetPak, LengthData, paket);//перепись пакета в массив пакетов
                }
                for (int i = 0; i < PocNum; i++)
                {
                    for (int j = 0; j > LengthData; j++)
                    {
                        pockets.Packet.DataRes[i, j] = DataRes[i, j];
                    }
                }
                return pockets;
            }
            catch(Exception err)
            {

                pockets.ErrorCode = err.HResult;
                pockets.ErrorDescription = err.Message;
                return pockets;
            }

        }
        /// <summary>
        /// Отправка пакетов с данными
        /// </summary>
        /// <param name="port">Порт, на который отправляется массив пакетов</param>
        /// <param name="ReadTimeOut">время ожидания</param>
        /// <returns>0 если отправка прошла успешно, 1 если отправка провалена</returns>
        public ErrorPacket SendData(SerialPort port, int ReadTimeOut)//отправка подготовленных пакетов==================================================================================================
        {
            ErrorPacket pockets = new ErrorPacket();
            try
            {
                pockets.ErrorCode = 0;
                pockets.ErrorDescription = "";
                port.ReadTimeout = ReadTimeOut;
                port.WriteTimeout = ReadTimeOut;
                for (int i = 0; i < PocNumDataSearch(DataRes); i++)//начало отправки пакетов
                {
                    LengthDataSearch(DataRes, i);  //определение объема данных в пакете
                    byte[] ArrOnSand = new byte[LengthData + 26];
                    for (int j = 0; j < LengthData + 26; j++)
                    {
                        ArrOnSand[j] = DataRes[i, j]; //перепись пакета в отправляемый массив
                    }
                    port.Write(ArrOnSand, 0, LengthData + 26); //отправка пакетов
                    CRSpaket = new byte[27];
                    bool crsNameCheck = false;
                    CRSpaket = new byte[27];
                    while (!crsNameCheck)
                    {
                        port.Read(CRSpaket, 0, 27);
                        string nameCRS = CRSNameSearch(CRSpaket);
                        if (nameCRS == Name)
                        {
                            crsNameCheck = true;
                            
                            if (CRSpaket[25] == 1)
                            {
                                i--;
                            }
                        }
                    }

                }
                pockets.ErrorCode = 0;
                pockets.ErrorDescription = "";
                return pockets;
            }
            catch(Exception err)
            {
                pockets.ErrorCode = err.HResult;
                pockets.ErrorDescription = err.Message;
                return pockets;
            }
        }

        /// <summary>
        /// Принятие массива пакетов
        /// </summary>
        /// <param name="port">потр, с которого будут приниматься пакеты</param>
        /// <param name="ReadTimeOut">Время ожидания пакетов</param>
        /// <returns>Код ошибки и её описание</returns>
        public ErrorPacket ReciveData(SerialPort port, int ReadTimeOut)//принятие пакетов================================================================================================
        {
            ErrorPacket pockets = new ErrorPacket();
            pockets.ErrorCode = 0;
            pockets.ErrorDescription = null;
            int colvoPacs = 0;
            int sizemas = 0;
            int sleep = 0;
            byte[] pok;
            bool checkONCRS;
            string namePaks = null;
            try
            {
                bool ErrorPac = true;
                while (ErrorPac)//начало принятия первого пакета
                {
                    sleep = 0;
                    while (sizemas < 1)//Ожидание получения первого пакета
                    {
                        sizemas = port.BytesToRead;
                        if (sleep == ReadTimeOut)
                        {
                            sleep = Convert.ToInt32("Вызов ошибки");//вызов ошибки если время ожидания вышло
                        }
                        sleep++;
                        Thread.Sleep(1);
                    }
                    pok = new byte[sizemas];
                    port.Read(pok, 0, sizemas);
                    DeConvertDataOutPaket(pok);//разборка пакетов на данные
                    namePaks = Name; //сохранение имени принятого пакета для дальнейшей проверки
                    colvoPacs = PocNum;
                    DataRes = new byte[PocNum, 626];
                    checkONCRS = CheckCRS(Name, PocCount, PocNum, LengthData, DataType, SenderName,ReciverName, Data, CRS);//проверка контрольной суммы
                    if (checkONCRS)
                    {
                        AssemblyCRS(0, SenderName, ReciverName);
                        port.Write(CRSpaket, 0, 27);             //подтверждение корректного получения пакетов
                        ErrorPac = false;
                        SaveDataInMatrix(PocCount, LengthData, pok);//сохранение в массив пакетов
                    }
                    else
                    {
                        AssemblyCRS(1, SenderName, ReciverName);
                        port.Write(CRSpaket, 0, 27);
                    }
                }
                if (colvoPacs > 1)//начало принятия остальных пакетов (если есть)
                {
                    for (int i = 1; i < colvoPacs; i++)
                    {
                        sizemas = port.BytesToRead;
                        sleep = 0;
                        while (sizemas < 1)//Ожидание получения пакета
                        {
                            sizemas = port.BytesToRead;
                            if (sleep == ReadTimeOut)
                            {
                                sleep = Convert.ToInt32("Вызов ошибки");//вызов ошибки если время ожидания вышло
                            }
                            sleep++;
                            Thread.Sleep(1);
                        }
                        pok = new byte[sizemas];
                        port.Read(pok, 0, sizemas);
                        DeConvertDataOutPaket(pok);
                        if (Name == namePaks)//проверка имени пришедшего пакета
                        {
                            checkONCRS = CheckCRS(Name, PocCount, PocNum, LengthData, DataType,SenderName,ReciverName, Data, CRS);//Проверка CRS
                            if (checkONCRS == true) // сравнение суммы данных
                            {
                                SaveDataInMatrix(PocCount, LengthData, pok); // сохранение данных в матрицу
                                AssemblyCRS(0, SenderName, ReciverName);
                                port.Write(CRSpaket,0,27); // оптравляет на другой конец порта 0, то что он расшифровал правильно пакет и готов дальше принимать 
                            }
                            else
                            {
                                i--;
                                AssemblyCRS(1, SenderName, ReciverName);
                                port.Write(CRSpaket, 0, 27); // отправляет на другой конец порта 1 на пересылку этого пакета, так как суммы не совпадают 
                            }
                        }
                        else
                        {
                            i--; //не то имя пакета
                        }
                    }
                }
                for (int i = 0; i < PocNum; i++)
                {
                    for (int j = 0; j > LengthData; j++)
                    {
                        pockets.Packet.DataRes[i, j] = DataRes[i, j];
                    }
                }
                return pockets; // Возврат отчета о ошибках
            }
            catch(Exception err)
            {

                pockets.ErrorCode = err.HResult;
                pockets.ErrorDescription = err.Message;
                return pockets; // Возврат отчета о ошибках
            }
        }
        /// <summary>
        /// Получение строки из массива пакетов
        /// </summary>
        /// <returns>Строка или ничего в случае ошибки</returns>
        public string ConvertPaketsInString()//Конвертация даты в строку=====================================================================================================================
        {
            try
            {
                string data = null;
                PocNumDataSearch(DataRes); // находит кол-во пакетов в 0 пакете
                for (int i = 0; i < PocNum; i++)
                {
                    LengthDataSearch(DataRes, i); // находит размеры данных в каждом пакете
                    byte[] array = new byte[LengthData]; // Создание массива для переписи данных из массива пакетов 
                    for (int j = 24; j < array.Length + 24; j++)
                    {
                        array[j - 24] = DataRes[i, j]; 
                    }
                    data += Encoding.UTF8.GetString(array); //конвертация данных пакета в строку и запись её в переменную
                }
                return data;//возврат полученной строки
            }
            catch
            {
                return null;//возврат "ничего" в случае какой либо ошибки
            }
        }
        /// <summary>
        /// поиск типа данных в пакете
        /// </summary>
        /// <returns>Возвращает номер типа данных, в случае ошибки возврашает номер данных типа Err</returns>
        public int DataTypeSearch()//поиск типа данных в пакете
        {
            try
            {
                byte[] fourArr = new byte[4];
                for (int s = 0; s < 4; s++) { fourArr[s] = DataRes[0, s + 12]; }
                int datatype = BitConverter.ToInt32(fourArr, 0);

                return datatype;
            }
            catch
            {
                return 5;
            }
        }
        private void AssemblyCRS(byte  Command, int SenderName, int ReciverName)
        {
            CRSpaket = new byte[27];

            int zero = 0;
            int one = 1;
            ushort crs;
            int datatype = 2;
            byte[] arrName = Encoding.UTF8.GetBytes(Name);
            byte[] arrPocCount = BitConverter.GetBytes(zero);
            byte[] arrPocNum = BitConverter.GetBytes(one);
            byte[] arrLenghtData = BitConverter.GetBytes(one);
            byte[] arrDataType = BitConverter.GetBytes(datatype);
            byte[] arrSenderName = BitConverter.GetBytes(SenderName);
            byte[] arrReciverName = BitConverter.GetBytes(ReciverName);

            byte[] arrData = new byte[1];
            arrData[0] = Command;

            crs = SchetCRS(arrName, arrPocCount, arrPocNum, arrLenghtData, arrDataType, arrSenderName, arrReciverName, arrData);
            byte[] arrCRS = BitConverter.GetBytes(crs);
            arrName.CopyTo(CRSpaket, 0);
            arrPocCount.CopyTo(CRSpaket, 2);
            arrPocNum.CopyTo(CRSpaket, 6);
            arrLenghtData.CopyTo(CRSpaket, 10);
            arrDataType.CopyTo(CRSpaket, 12);
            arrSenderName.CopyTo(CRSpaket, 16);
            arrReciverName.CopyTo(CRSpaket, 20);
            arrData.CopyTo(CRSpaket, 24);
            arrCRS.CopyTo(CRSpaket, arrData.Length+1);

        }
        private string CRSNameSearch(byte[] pakCRS)
        {
            byte[] twoArr = new byte[2];
            twoArr[0] = pakCRS[0];
            twoArr[1] = pakCRS[1];
            string name = Encoding.UTF8.GetString(twoArr);
            return name;
        }
    }
}
