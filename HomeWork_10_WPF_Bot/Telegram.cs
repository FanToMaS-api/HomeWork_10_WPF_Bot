using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using HomeWork_10_WPF_Bot;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace HomeWork_10_WPF_Bot
{
    internal class Telegram
    {
        private static string token = File.ReadAllText(@"C:\Users\ти\source\repos\TelegramToken.txt"); // Токен в телеге
        // коллекция для вывода сообщений, присылаемых боту на окно
        public ObservableCollection<Messages> messages;
        // словарь, содержащий всех пользователей
        private static Dictionary<long, string> usersId = new Dictionary<long, string>(); 
        private TelegramBotClient bot;
        private MainWindow window;
        static bool userTextAvailible = false;
        /// <summary>
        /// Конструктор для запуска работы бота
        /// </summary>
        /// <param name="W"> Окно для вывода информации, присылаемой боту</param>
        public Telegram(MainWindow W)
        {
            window = W;
            bot = new TelegramBotClient(token);
            messages = new ObservableCollection<Messages>();
            CreateDictionaryOfUsersId(); // загружает пользователей уже писавших боту в словарь
            bot.OnMessage += MessageListener;
            bot.StartReceiving();
            
        }
        /// <summary>
        /// Событие возникающее при получении новых смс ботом
        /// Содержит всю основную логику программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MessageListener(object sender, global::Telegram.Bot.Args.MessageEventArgs e)
        {

            // Добавление на панель сообщения, как только сработало событие прихо
            window.Dispatcher.Invoke(() =>
                messages.Add(
                    new Messages(e.Message.Chat.FirstName, e.Message.Text,
                    e.Message.Chat.Id, e.Message.Date.ToShortTimeString())));

            if (e.Message.Text != null && userTextAvailible != true)
                SendMail(e.Message.Chat.FirstName, e.Message.Text, "");
            if (userTextAvailible)
            {
                userTextAvailible = false;
                SendMail(e.Message.Chat.FirstName, e.Message.Text, "UserText.txt");
                bot.SendTextMessageAsync(e.Message.Chat.Id, "Спасибо, твой текст принят на обработку!");
                bot.SendTextMessageAsync(e.Message.Chat.Id, "Если снова понадобится помощь, просто пришли файл в формате .txt");
            }

            if (!usersId.ContainsKey(e.Message.Chat.Id))
            {
                usersId.Add(e.Message.Chat.Id, e.Message.Chat.FirstName);
                SaveJsonUsersId();
            }

            if (e.Message.Type == global::Telegram.Bot.Types.Enums.MessageType.Text)
            {

                if (e.Message.Text == "/start")
                {
                    string responseText = $"Привет, {e.Message.Chat.FirstName}! Мы обновились!" +
                        $" Теперь я могу помочь тебе найти все кафе, рестораны, столовые и т.п.\n" +
                        $"Достаточно сказать мне улицу/проспект/площадь и даже дом!\n" +
                        $"Только в случае с домом за точность не отвечаю - их уж очень много в Москве)";
                    bot.SendTextMessageAsync(e.Message.Chat.Id, responseText);
                }
                else if (e.Message.Text.Length >= 4)
                {
                    WorkWithMosRu(e.Message.Text, e.Message.Chat.Id);
                }
                else
                    bot.SendTextMessageAsync(e.Message.Chat.Id, $"Детка! Мне нужно название длиннее <<{e.Message.Text}>>");
            }
            else if (e.Message.Type == global::Telegram.Bot.Types.Enums.MessageType.Document)
                Download(e.Message.Document.FileId, e.Message.Chat.Id, e.Message.Chat.FirstName, e.Message.Document.FileName);
            else if (e.Message.Type == global::Telegram.Bot.Types.Enums.MessageType.Photo)
                Download(e.Message.Photo[e.Message.Photo.Length-1].FileId, e.Message.Chat.Id, 
                    e.Message.Chat.FirstName, "Photo");
            else
                bot.SendTextMessageAsync(e.Message.Chat.Id, "Наверное ты промахнулся и прислал не тот файл. Попробуй еще раз!)");

        }
        /// <summary>
        /// Функция отвечающая за отправку файлов пользователю и загрузку их от него
        /// + идет проверка на автора всех .txt файлов
        /// </summary>
        /// <param name="fileId"> Id файла на серверах телеги</param>
        /// <param name="chatId"> Id чата</param>
        /// <param name="FirstNameOfUser"> Имя пользователя</param>
        /// <param name="FileName"> Имя файла, полученного от пользователя</param>
        private async void Download(string fileId, long chatId, string FirstNameOfUser, string FileName)
        {
            var file = await bot.GetFileAsync(fileId);
            var r = new Random();
            string[] buff = new string[] {
                "Получи ответочку!;)",
                "Забери, ты обронил...",
                "Ах ты, шалун! Присылаешь тут всякую чушь)) Держи обратно!" };
            await bot.SendTextMessageAsync(chatId, buff[r.Next(0, 3)]);

            var path = @"C:\Users\ти\source\repos\HomeWork_10_WPF_Bot\HomeWork_10_WPF_Bot\bin\Debug\Пользовательские файлы\";
            FileStream fs = new FileStream($"{path}{FileName}{Path.GetExtension(file.FilePath)}", FileMode.Create);
            await bot.DownloadFileAsync(file.FilePath, fs);
            fs.Close();
            fs.Dispose();
            SendMail(FirstNameOfUser, "", $"{path}{FileName}{Path.GetExtension(file.FilePath)}");
            if (Path.GetExtension(file.FilePath).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase)
                || Path.GetExtension(file.FilePath).Equals(".png", StringComparison.InvariantCultureIgnoreCase)
                || Path.GetExtension(file.FilePath).Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase))
            {
                await bot.SendPhotoAsync(chatId, fileId);
            }
            else
                //if (Path.GetExtension(file.FilePath).Equals(".pdf", StringComparison.InvariantCultureIgnoreCase)
                //    || Path.GetExtension(file.FilePath).Equals(".docx", StringComparison.InvariantCultureIgnoreCase)
                //    || Path.GetExtension(file.FilePath).Equals(".txt", StringComparison.InvariantCultureIgnoreCase)
                //    || Path.GetExtension(file.FilePath).Equals(".rar", StringComparison.InvariantCultureIgnoreCase)
                //    || Path.GetExtension(file.FilePath).Equals(".zip", StringComparison.InvariantCultureIgnoreCase)
                //    || Path.GetExtension(file.FilePath).Equals(".xlsx", StringComparison.InvariantCultureIgnoreCase))
                await bot.SendDocumentAsync(chatId, fileId);
            //}                        
        }
        /// <summary>
        /// Отправляет на указанную почту все сообщения и файлы, пришедшие  боту
        /// </summary>
        /// <param name="nameOfUser"> Имя пользователя</param>
        /// <param name="message"> Его сообщение</param>
        /// <param name="path"> Путь к файлу</param>
        private void SendMail(string nameOfUser, string message, string path)
        {

            MailAddress from = new MailAddress("misha.fuuuux@gmail.com");
            string password = "70Aroper";
            MailAddress to = new MailAddress("misha.dulov@mail.ru");
            MailMessage mailMessage = new MailMessage(from, to);
            mailMessage.Subject = $"{DateTime.Now}"; //тема письма
            if (path != "")
            {
                mailMessage.Attachments.Add(new Attachment(path));
            }
            mailMessage.Body = $"<h4>{nameOfUser} {message}</h4>";  // текст письма h4 - размер
            mailMessage.IsBodyHtml = true;  // письмо представляет код html
            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);  // адрес smtp-сервера и порт, с которого будем отправлять письмо
            smtp.Credentials = new NetworkCredential("misha.fuuuux@gmail.com".Split('@')[0], password);  // логин и пароль
            smtp.EnableSsl = true;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            mailMessage.BodyEncoding = Encoding.UTF8;
            smtp.Send(mailMessage);
            mailMessage.Dispose();
        }
        /// <summary>
        /// Создает базу данных пользователей, которые писали боту
        /// </summary>
        private void SaveJsonUsersId()
        {
            JObject mainTree = new JObject();
            JArray usersIdArray = new JArray();
            foreach (var e in usersId)
            {
                JObject user = new JObject();
                user["ChatId"] = e.Key;
                user["FirstName"] = e.Value;
                usersIdArray.Add(user);
            }
            mainTree["ArrayOfUsersId"] = usersIdArray;
            using (StreamWriter sw = new StreamWriter("UsersId.json", false))
            {
                sw.WriteLineAsync(mainTree.ToString());
            }
        }
        /// <summary>
        /// Создаю словарь состоящий из имен пользователей и их chatId
        /// </summary>
        private void CreateDictionaryOfUsersId()
        {
            using (StreamReader sr = new StreamReader("UsersId.json"))
            {
                string json = sr.ReadToEnd();
                var ArrayOfId = JObject.Parse(json)["ArrayOfUsersId"].ToArray();
                foreach (var e in ArrayOfId)
                    usersId.Add(long.Parse(e["ChatId"].ToString()), e["FirstName"].ToString()); // выгружаю из базы пользователей
            }
        }
        /// <summary>
        /// Возвращает пользователю блок ближайших кафе рядом с его местоположением
        /// </summary>
        /// <param name="street"> Название улицы, на которой расположен пользователь</param>
        /// <param name="chatId"> Уникальный номер чата с пользователем</param>
        private void WorkWithMosRu(string street, long chatId)
        {
            #region Подготовка к работе с порталом
            string tokenMosRu = File.ReadAllText(@"C:\Users\ти\source\repos\MosRuToken.txt"); // токен на сайте Mos.ru
            WebClient wc = new WebClient();
            string versionOfPortal = JObject.Parse(wc.DownloadString(@"https://apidata.mos.ru/version"))["Version"].ToString(); // получаю версию портала
            string startUrl = @"https://apidata.mos.ru/v";
            string idDataSet = "1903"; // уникальный номер базы данных
            string ReleaseNumber = JObject.Parse(wc.DownloadString($"{startUrl}{versionOfPortal}/datasets/{idDataSet}/version?&api_key={tokenMosRu}"))["releaseNumber"].ToString();
            string VersionNumber = JObject.Parse(wc.DownloadString($"{startUrl}{versionOfPortal}/datasets/{idDataSet}/version?&api_key={tokenMosRu}"))["versionNumber"].ToString();
            string nextUrl = $"{versionOfPortal}/datasets/{idDataSet}/rows?$filter=Cells/Address eq {street.Replace(" ", "%20")}&$top=3" +
                $"&versionNumber={VersionNumber}&releaseNumber={ReleaseNumber}";
            #endregion
            try
            {
                string json = wc.DownloadString($"{startUrl}{nextUrl}&api_key={tokenMosRu}");
                byte[] bytes = Encoding.Default.GetBytes(json);
                json = Encoding.UTF8.GetString(bytes);
                if (json == "[]")
                    bot.SendTextMessageAsync(chatId, "К сожалению, я не смог найти заведения рядом с тобой(\n" +
                  "Проверь правильность в названии улицы и попробуй еще раз, я постараюсь помочь)");
                else
                {
                    bot.SendTextMessageAsync(chatId, "Вот те места которые располагаются рядом с тобой\n");
                    //Thread.Sleep(2000);
                    var array = JArray.Parse(json).ToArray();
                    foreach (var e in array)
                    {
                        bot.SendTextMessageAsync(chatId, $"Название: {e["Cells"]["Name"]}\n" +
                            $"Адрес: { e["Cells"]["Address"]}\n" +
                            $"Кол-во мест: {e["Cells"]["SeatsCount"]}\n" +
                            $"Тип: {e["Cells"]["TypeObject"]}\n");
                    }
                }
            }
            catch (Exception)
            {
                bot.SendTextMessageAsync(chatId, "К сожалению, я не смог найти заведения рядом с тобой(\n" +
                   "Попроверь правильность в названии улицы и попробуй еще раз, я постараюсь помочь)");
            }
        }
        /// <summary>
        /// Отправляет письмо одному пользователю при indicator = false
        /// или всем пользователям забитым в базе при indicator = true
        /// </summary>
        /// <param name="text"> Сообщение</param>
        /// <param name="userId"> Id для одного пользователя</param>
        /// <param name="indicator"> Индикатор (служит для определения режима отправки)</param>
        public void SendMessage(string text, string userId, bool indicator)
        {
            if(!indicator)
            bot.SendTextMessageAsync(userId, text);
            else
            {
                foreach(var e in usersId)
                {
                    bot.SendTextMessageAsync(e.Key, text);
                }
            }
        }
    }
    
}