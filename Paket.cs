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
        public Packet Packet = new Packet();
        public Values Values = new Values();
    }

    public class Packet
    {
        //public byte[,] DataRes; //индекс 1-номер пакета, индекс 2-номер байта
        //private string Name; //2 символа 
        //private int PocCount; //4 байта
        //private int PocNum; //4 байта
        //private ushort LengthData;//2 байта
        //private int DataType; //4 байта
        //private int SenderName; //4 байта
        //private int ReciverName; //4 байта
        //private byte[] Data;//до 600 байт
        //private ushort CRS; //2 байта
        //private byte[] paket;
        //private byte[] CRSpaket;

        private const byte PointPocCount = 2;
        private const byte PointPocNum = 6;
        private const byte PointLengthData = 10;
        private const byte PointDataType = 12;
        private const byte PointSenderName = 16;
        private const byte PointReciverName = 20;
        private const byte PointData = 24;
        private const byte SizeEmptyPak = 26;
        private const byte SizeCRSpak = 27;
        private const ushort MaxSizeData = 600;
        private const ushort MaxSizePacket = 626;
        private const byte CommandByte = 25;

        /// <summary>
        /// Определение количества пакетов в массиве пакетов
        /// </summary>
        /// <param name="DataRes">Массив пакетов</param>
        /// <returns>Количество пакетов</returns>
        private int PocNumDataSearch(byte[,] DataRes)
        {
            byte[] fourArr = new byte[4];
            for (int s = 0; s < 4; s++) { fourArr[s] = DataRes[0, s + PointPocNum]; }
            int pocNum = BitConverter.ToInt32(fourArr, 0);
            return pocNum;
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
            for (int j = 0; j < 2; j++) { twoArr[j] = DataRes[num, j + PointLengthData] ; }
            ushort lengthData = BitConverter.ToUInt16(twoArr, 0);
            return lengthData;
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
        private Values ConvertDataInPaket(Values val)//конвертация данных в массив байтов------------------------------------------
        {
            byte[] arrName = Encoding.UTF8.GetBytes(val.Name);
            byte[] arrPocCount = BitConverter.GetBytes(val.PocCount);
            byte[] arrPocNum = BitConverter.GetBytes(val.PocNum);
            byte[] arrLenghtData = BitConverter.GetBytes(val.LengthData);
            byte[] arrDataType = BitConverter.GetBytes(val.DataType);
            byte[] arrSenderName = BitConverter.GetBytes(val.SenderName);
            byte[] arrReciverName = BitConverter.GetBytes(val.ReciverName);
            val.CRS = SchetCRS(arrName, arrPocCount, arrPocNum, arrLenghtData, arrDataType, arrSenderName, arrReciverName, val.Data);
            byte[] arrCRS = BitConverter.GetBytes(val.CRS);
            val.paket = new byte[arrName.Length + arrPocCount.Length + arrPocNum.Length + arrLenghtData.Length + arrDataType.Length + arrCRS.Length + val.Data.Length+ arrSenderName.Length+arrReciverName.Length];
            arrName.CopyTo(val.paket, 0);
            arrPocCount.CopyTo(val.paket, PointPocCount);
            arrPocNum.CopyTo(val.paket, PointPocNum);
            arrLenghtData.CopyTo(val.paket, PointLengthData);
            arrDataType.CopyTo(val.paket, PointDataType);
            arrSenderName.CopyTo(val.paket,PointSenderName);
            arrReciverName.CopyTo(val.paket,PointReciverName);
            val.Data.CopyTo(val.paket, PointData);
            arrCRS.CopyTo(val.paket, val.Data.Length + PointData);
            return val;
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
        private Values DeConvertDataOutPaket(byte[] readbyte)//деконвертация данных из массива байтов-------------------------------------------------,----------------------------------------
        {
            Values val = new Values();
            byte[] fourArr = new byte[4];
            byte[] twoArr = new byte[2];

            for (int i = 0; i < 2; i++) { twoArr[i] = readbyte[i]; }
            val.Name = Encoding.UTF8.GetString(twoArr);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + PointPocCount]; }
            val.PocCount = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + PointPocNum]; }
            val.PocNum = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 2; i++) { twoArr[i] = readbyte[i + PointLengthData]; }
            val.LengthData = BitConverter.ToUInt16(twoArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + PointDataType]; }
            val.DataType = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + PointSenderName]; }
            val.SenderName = BitConverter.ToInt32(fourArr, 0);

            for (int i = 0; i < 4; i++) { fourArr[i] = readbyte[i + PointReciverName]; }
            val.ReciverName = BitConverter.ToInt32(fourArr, 0);

            int Numdata = PointData;
            val.Data = new byte[val.LengthData];
            for (int i = 0; i < val.LengthData; i++)
            {
                val.Data[i] = readbyte[Numdata];
                Numdata++;
            }

            for (int i = 0; i < 2; i++) { twoArr[i] = readbyte[Numdata + i]; }
            val.CRS = BitConverter.ToUInt16(twoArr, 0);
            return val;
        }

        /// <summary>
        /// Сохранение указанного массива в массив пакетов
        /// </summary>
        /// <param name="schetPak">Номер пакета</param>
        /// <param name="lengthData">Объем данных</param>
        /// <param name="pakets">Массив из которого будут переписаны данные</param>
        private Values SaveDataInMatrix(Values val, byte[] paket, int PocCount)//сохранение массива на указанную строку------------------------------------------------------------------
        {
            for (int i = 0; i < val.LengthData + SizeEmptyPak; i++)
            {
                val.DataRes[PocCount, i] = paket[i];
            }
            return val;
        }
        /// <summary>
        /// Сохранение массива пакетов в фаил
        /// </summary>
        /// <param name="fileName">Путь к файлу</param>
        /// <returns>Код ошибки и её описание</returns>
        public ErrorPacket SaveDataInFile(string fileName, Values val)//сохранение в фаил===============================
        {
            ErrorPacket err = new ErrorPacket();
            try
            {
                err.ErrorCode = 0;
                err.ErrorDescription = "";
                int AllDataL = 0;
                int PocN = PocNumDataSearch(val.DataRes);
                int schetByte = 0;
                for (int i = 0; i < PocN; i++)
                {
                    int length = LengthDataSearch(val.DataRes, i);
                    AllDataL += length;
                }
                byte[] saveArray = new byte[AllDataL];
                for (int i = 0; i < PocN; i++)
                {
                    int DataL = LengthDataSearch(val.DataRes, i);
                    for (int j = 24; j < DataL + PointData; j++)
                    {
                        saveArray[schetByte] = val.DataRes[i, j];
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
            ErrorPacket pockets = new ErrorPacket
            {
                ErrorCode = 0,
                ErrorDescription = null
            };
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
            ErrorPacket pockets = new ErrorPacket();
            pockets.Values.SenderName = senName;
            pockets.Values.ReciverName = recName;
            pockets.Values.DataType = (int)type;
            try
            {
                pockets.ErrorCode = 0;
                pockets.ErrorDescription = null;
                Random rand = new Random();
                string nameRand = Convert.ToString(rand.Next(10, 99));//генерация имени пакетов

                int schetBytes = 0;
                int ostatok = 0;
                int schetPak = 0;
                if (byteArray.Length < MaxSizeData)         //
                {                                   //
                    ostatok = 1;                    //
                }                                   // определение наличие пакета,, объем которого меньше 600
                if (byteArray.Length % MaxSizeData > 0)     //
                {                                   //
                    ostatok = 1;                    //
                }
                int pacKolvo = byteArray.Length / MaxSizeData; //определение количества пакетов объемом в 600 байт
                pockets.Values.PocNum = pacKolvo + ostatok;//подсчет количества пакетов кратных 600 и пакета не кратного 600
                pockets.Values.DataRes = new byte[pockets.Values.PocNum, MaxSizePacket];

                if (byteArray.Length > MaxSizeData)//начало отправки пакетов с данными, объем которых = 600
                {
                    for (int schet = 0; schet < pacKolvo; schet++)
                    {
                        pockets.Values.Name = nameRand;
                        pockets.Values.PocCount = schetPak;

                        pockets.Values.LengthData = MaxSizeData;
                        pockets.Values.Data = new byte[pockets.Values.LengthData];
                        for (int i = 0; i < MaxSizeData; i++)
                        {
                            pockets.Values.Data[i] = byteArray[schetBytes];
                            schetBytes++;
                        }
                        pockets.Values = ConvertDataInPaket(pockets.Values);
                        pockets.Values = SaveDataInMatrix(pockets.Values, pockets.Values.paket,schetPak);
                        schetPak++;
                    }
                }
                if (ostatok == 1 || byteArray.Length < MaxSizeData)//Отправка пакета объемом <600 если таковой имеется
                {
                    pockets.Values.Name = nameRand;
                    pockets.Values.PocCount = schetPak;
                    pockets.Values.PocNum = pacKolvo + ostatok;
                    pockets.Values.LengthData = Convert.ToUInt16(byteArray.Length % MaxSizeData);
                    pockets.Values.Data = new byte[pockets.Values.LengthData];
                    for (int i = 0; i < byteArray.Length % MaxSizeData; i++)
                    {
                        pockets.Values.Data[i] = byteArray[schetBytes];
                        schetBytes++;
                    }
                    pockets.Values = ConvertDataInPaket(pockets.Values);
                    pockets.Values = SaveDataInMatrix(pockets.Values, pockets.Values.paket,schetPak);
                }
                return pockets;
            }
            catch (Exception err)
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
        public ErrorPacket SendData(SerialPort port, int ReadTimeOut, ErrorPacket pockets)//отправка подготовленных пакетов==================================================================================================
        {
            try
            {
                pockets.ErrorCode = 0;
                pockets.ErrorDescription = "";
                port.ReadTimeout = ReadTimeOut;
                port.WriteTimeout = ReadTimeOut;
                for (int i = 0; i < PocNumDataSearch(pockets.Values.DataRes); i++)//начало отправки пакетов
                {
                    pockets.Values.LengthData = LengthDataSearch(pockets.Values.DataRes, i);  //определение объема данных в пакете
                    byte[] ArrOnSand = new byte[pockets.Values.LengthData + SizeEmptyPak];
                    for (int j = 0; j < pockets.Values.LengthData + SizeEmptyPak; j++)
                    {
                        ArrOnSand[j] = pockets.Values.DataRes[i, j]; //перепись пакета в отправляемый массив
                    }
                    port.Write(ArrOnSand, 0, pockets.Values.LengthData + SizeEmptyPak); //отправка пакетов
                    pockets.Values.CRSpaket = new byte[SizeCRSpak];
                    bool crsNameCheck = false;
                    pockets.Values.CRSpaket = new byte[SizeCRSpak];
                    crsNameCheck = false;
                    while (!crsNameCheck)
                    {
                        port.Read(pockets.Values.CRSpaket, 0, SizeCRSpak);
                        string nameCRS = CRSNameSearch(pockets.Values.CRSpaket);
                        if (nameCRS == pockets.Values.Name)
                        {
                            crsNameCheck = true;
                            
                            if (pockets.Values.CRSpaket[25] == 1)
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
                    pockets.Values=DeConvertDataOutPaket(pok);//разборка пакетов на данные
                    namePaks = pockets.Values.Name; //сохранение имени принятого пакета для дальнейшей проверки
                    colvoPacs = pockets.Values.PocNum;
                    pockets.Values.DataRes = new byte[pockets.Values.PocNum, MaxSizePacket];
                    checkONCRS = CheckCRS(pockets.Values.Name, pockets.Values.PocCount, pockets.Values.PocNum, pockets.Values.LengthData, pockets.Values.DataType, pockets.Values.SenderName, pockets.Values.ReciverName, pockets.Values.Data, pockets.Values.CRS);//проверка контрольной суммы
                    if (checkONCRS)
                    {
                        pockets.Values = AssemblyCRS(0, pockets.Values);
                        port.Write(pockets.Values.CRSpaket, 0, SizeCRSpak);             //подтверждение корректного получения пакетов
                        ErrorPac = false;
                        pockets.Values=SaveDataInMatrix(pockets.Values, pok,0);//сохранение в массив пакетов
                    }
                    else
                    {
                        pockets.Values=AssemblyCRS(1, pockets.Values);
                        port.Write(pockets.Values.CRSpaket, 0, SizeCRSpak);
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
                        if (pockets.Values.Name == namePaks)//проверка имени пришедшего пакета
                        {
                            checkONCRS = CheckCRS(pockets.Values.Name, pockets.Values.PocCount, pockets.Values.PocNum, pockets.Values.LengthData, pockets.Values.DataType, pockets.Values.SenderName, pockets.Values.ReciverName, pockets.Values.Data, pockets.Values.CRS);//Проверка CRS
                            if (checkONCRS)
                            {
                                pockets.Values = AssemblyCRS(0, pockets.Values);
                                port.Write(pockets.Values.CRSpaket, 0, SizeCRSpak);             //подтверждение корректного получения пакетов
                                pockets.Values = SaveDataInMatrix(pockets.Values, pok,i);//сохранение в массив пакетов
                            }
                            else
                            {
                                pockets.Values = AssemblyCRS(1, pockets.Values);
                                port.Write(pockets.Values.CRSpaket, 0, SizeCRSpak);
                                i--;
                            }
                        }
                        else
                        {
                            i--; //не то имя пакета
                        }
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
        public string ConvertPaketsInString(Values val)//Конвертация даты в строку=====================================================================================================================
        {
            try
            {
                string data = null;
                PocNumDataSearch(val.DataRes); // находит кол-во пакетов в 0 пакете
                for (int i = 0; i < val.PocNum; i++)
                {
                    LengthDataSearch(val.DataRes, i); // находит размеры данных в каждом пакете
                    byte[] array = new byte[val.LengthData]; // Создание массива для переписи данных из массива пакетов 
                    for (int j = PointData; j < array.Length + PointData; j++)
                    {
                        array[j - PointData] = val.DataRes[i, j]; 
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
        public int DataTypeSearch(Values val)//поиск типа данных в пакете
        {
            try
            {
                byte[] fourArr = new byte[4];
                for (int s = 0; s < 4; s++) { fourArr[s] = val.DataRes[0, s + PointDataType]; }
                int datatype = BitConverter.ToInt32(fourArr, 0);

                return datatype;
            }
            catch
            {
                return 5;
            }
        }
        private Values AssemblyCRS( byte command,Values val)
        {
            val.CRSpaket = new byte[SizeCRSpak];

            ushort crs;
            byte[] arrName = Encoding.UTF8.GetBytes(val.Name);
            byte[] arrPocCount = BitConverter.GetBytes(0);
            byte[] arrPocNum = BitConverter.GetBytes(1);
            byte[] arrLenghtData = BitConverter.GetBytes(1);
            byte[] arrDataType = BitConverter.GetBytes((int)TypesContent.CRS);
            byte[] arrSenderName = BitConverter.GetBytes(val.SenderName);
            byte[] arrReciverName = BitConverter.GetBytes(val.ReciverName);

            byte[] arrData = new byte[1];
            arrData[0] = command;

            crs = SchetCRS(arrName, arrPocCount, arrPocNum, arrLenghtData, arrDataType, arrSenderName, arrReciverName, arrData);
            byte[] arrCRS = BitConverter.GetBytes(crs);
            arrName.CopyTo(val.CRSpaket, 0);
            arrPocCount.CopyTo(val.CRSpaket, PointPocCount);
            arrPocNum.CopyTo(val.CRSpaket, PointPocNum);
            arrLenghtData.CopyTo(val.CRSpaket, PointLengthData);
            arrDataType.CopyTo(val.CRSpaket, PointDataType);
            arrSenderName.CopyTo(val.CRSpaket, PointSenderName);
            arrReciverName.CopyTo(val.CRSpaket, PointReciverName);
            arrData.CopyTo(val.CRSpaket, PointData);
            arrCRS.CopyTo(val.CRSpaket, arrData.Length+PointData);
            return val;
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
