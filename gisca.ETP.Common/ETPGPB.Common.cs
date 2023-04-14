using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ETPGPB.Common.Authentication;

namespace ETPGPB.Common
{
    public abstract class ClientBase
    {
        /// <summary>
        /// Коннектор, через который работаем
        /// </summary>
        protected IExchangeServiceConnector serviceClient;

        /// <summary>
        /// Получить список контактов организации.
        /// Список контактов включает в себя все контакты организации, в том числе те, с которыми обмен запрещен.
        /// </summary>
        /// <param name="organizationId">ИД организации</param>
        /// <param name="AuthenticationToken">Токен Сессии</param>
        /// <param name="lastSync">Дата последней авторизации</param>
        /// <returns>Список контрагентов организации.</returns>
        public static List<ContactAdressBook> GetContacts(string organizationId, string AuthenticationToken, System.DateTime lastSync)
        {
            return Participatin.GetContactList(organizationId, AuthenticationToken, lastSync);
        }



        /// <summary>
        /// Событие истечения срока действия токена авторизации.
        /// </summary>
        public event EventHandler<TokenExpiredEventArgs> TokenExpired;

        /// <summary>
        /// Инициализация ETP.
        /// </summary>
        public static void Initialize(LogDelegate logCallback, LogExceptionDelegate logExCallback)
        {
            if (logCallback == null)
            {
                throw new ArgumentNullException("logCallback");
            }
            if (logExCallback == null)
            {
                throw new ArgumentNullException("logExCallback");
            }
            Log.Initialize(logCallback, logExCallback);
            Log.Trace("Модуль ETP инициализирован");
        }


        /// <summary>
        /// Инициализировать прокси по-умолчанию.
        /// </summary>
        private static void InitializeDefaultProxy()
        {
            WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
            WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
        }



        /// <summary>
        /// Класс по умолчанию для доступа к токену.
        /// </summary>
        private class DefaultAuthTokenProvider : IAuthTokenProvider
        {
            public string AuthenticationToken { get; set; }
        }

        /// <summary>
        /// Найти и создать коннектор к системе обмена.
        /// </summary>
        /// <param name="clientSystem">Система обмена.</param>
        protected abstract IExchangeServiceConnector ResolveConnector(ExchangeSystem clientSystem);


        /// <summary>
        /// Обновить токен авторизации.
        /// </summary>
        /// <returns>Признак того, что токен был обновлен.</returns>
        protected bool UpdateToken()
        {
            TokenExpiredEventArgs args = new TokenExpiredEventArgs();
            this.TokenExpired?.Invoke(this, args);
            return args.TokenUpdated;
        }
    }


    public class ConnectorSettings
    {
        public bool ExtendedLogging { get; set; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        public ConnectorSettings()
        {
        }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="extendedLogging"></param>
        public ConnectorSettings(bool extendedLogging)
        {
            ExtendedLogging = extendedLogging;
        }
    }


    public class Contact : IContact
    {
        public Organization Organization { get; set; }

        public ContactStatus Status { get; set; }

        public DateTime? StatusChangeDate { get; set; }

        public string Comment { get; set; }
    }

    /// <summary>
    /// Контакт с контрагентом.
    /// </summary>
    public interface IContact
    {
        /// <summary>
        /// Организация контрагента.
        /// </summary>
        Organization Organization { get; }

        /// <summary>
        /// Состояние обмена с контрагентом.
        /// </summary>
        ContactStatus Status { get; }

        /// <summary>
        /// Дата изменения статуса в формате UTC.
        /// </summary>
        DateTime? StatusChangeDate { get; }

        /// <summary>
        /// Комментарий контрагента.
        /// </summary>
        string Comment { get; }

    }

    /// <summary>
    /// Коннектор к сервису обмена.
    /// </summary>
    public interface IExchangeServiceConnector
    {
        /// <summary>
        /// Имя сервиса.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Поддерживаемые операции работы с контактами.
        /// </summary>
        //ContactCapabilities ContactCapabilities { get; }

        /// <summary>
        /// Признак возможности синхронизировать контакты с сервиса обмена.
        /// </summary>
        bool CanSynchronizeContacts { get; }

        /// <summary>
        /// Признак того, что для коннектора можно получить сертификаты нашей организации.
        /// </summary>
        /// <returns>True, если сертификаты можно получить, иначе False.</returns>
        bool CanGetOurSubscriberCertificates { get; }

        /// <summary>
        /// Инициализировать коннектор.
        /// </summary>
        /// <param name="serviceSettings">Настройки коннектора.</param>
        /// <param name="authTokenProvider">Провайдер токена аутентификации.</param>
        /// <param name="tokenExpiredHandler">Обработчик события на истечение срока действия токена аутентификации.</param>
        /// <param name="connectorSetting">Расширенные настройки подключения к коннектору.</param>
        void Initialize(ServiceSettings serviceSettings, IAuthTokenProvider authTokenProvider, Func<bool> tokenExpiredHandler, ConnectorSettings connectorSetting);

        /// <summary>
        /// Выполнить аутентификацию на сервисе обмена по логину и паролю.
        /// </summary>
        /// <param name="login">Логин пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        /// <returns>Токен авторизации.</returns>
        /// <exception cref="T:NpoComputer.DCX.Common.Exceptions.AuthenticationException">Некорректный логин/пароль</exception>
        string AuthenticateByPassword(string login, string password);

        /// <summary>
        /// Выполнить аутентификацию на сервисе обмена по сертификату.
        /// </summary>
        /// <param name="certificate">Cертификат.</param>
        /// <returns>Зашифрованный токен авторизации.</returns>
        /// <remarks>
        /// Не расшифровывает токен, даже если в параметре передаётся сертификат с закрытым ключом.
        /// Для использования токена его нужно расшифровать с помощью сертификата, который использовался при авторизации.
        /// </remarks>
        /// <exception cref="T:NpoComputer.DCX.Common.Exceptions.AuthenticationException">Некорректный сертификат</exception>
        string AuthenticateByCertificate(X509Certificate2 certificate);

        /// <summary>
        /// Выполнить аутентификацию на сервисе обмена по сертификату.
        /// </summary>
        /// <param name="certificateContent">Cертификат в виде байтового массива.</param>
        /// <remarks>
        /// Не расшифровывает токен, даже если в параметре передаётся сертификат с закрытым ключом.
        /// Для использования токена его нужно расшифровать с помощью сертификата, который использовался при авторизации.
        /// </remarks>
        /// <exception cref="T:NpoComputer.DCX.Common.Exceptions.AuthenticationException">Некорректный сертификат</exception>
        string AuthenticateByCertificate(byte[] certificateContent);

        /// <summary>
        /// Получить список контактов организации.
        /// </summary>
        /// <param name="boxId">ИД ящика нашей организации.</param>
        /// <param name="ourOrganizationId">ИД нашей организации на сервисе.</param>
        /// <param name="changedAfterDate">Дата последнего изменения контактов.
        /// Если указана, будут выбраны только те контакты, которые были измененны после заданной даты.
        /// При сравнении дата будет преобразовываться в формат UTC.</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Список контактов организации.</returns>
        List<IContact> GetContactList(string boxId, string ourOrganizationId, DateTime? changedAfterDate, CancellationToken cancellationToken);

        /// <summary>
        /// Разорвать контакт с контрагентом.
        /// </summary>
        /// <param name="ourOrganizationId">ИД нашей организации.</param>
        /// <param name="counteragentId">ИД контрагента.</param>
        /// <param name="comment">Комментарий.</param>
        void BreakWithCounteragent(string ourOrganizationId, string counteragentId, string comment);

        /// <summary>
        /// Отправить организации запрос на участие в обмене документами.
        /// </summary>
        /// <param name="ourOrganizationId">ИД нашей организации.</param>
        /// <param name="counteragentId">ИД контрагента.</param>
        /// <param name="comment">Комментарий.</param>
        void SendInvitationRequest(string ourOrganizationId, string counteragentId, string comment);

        /// <summary>
        /// Принять приглашение контрагента.
        /// </summary>
        /// <param name="ourOrganizationId">ИД нашей организации.</param>
        /// <param name="counteragentId">ИД контрагента.</param>
        /// <param name="comment">Комментарий.</param>
        void AcceptInvitation(string ourOrganizationId, string counteragentId, string comment);

        /// <summary>
        /// Отклонить приглашение контрагента.
        /// </summary>
        /// <param name="ourOrganizationId">ИД нашей организации.</param>
        /// <param name="counteragentId">ИД контрагента.</param>
        /// <param name="comment">Комментарий.</param>
        void RejectInvitation(string ourOrganizationId, string counteragentId, string comment);

        /// <summary>
        /// Получить список организаций по ИНН и КПП.
        /// </summary>
        /// <param name="inn">ИНН организации.</param>
        /// <param name="kpp">КПП организации (может отсутствовать).</param>
        /// <returns>Список организаций с заданными ИНН и КПП.</returns>
        List<Organization> FindOrganizationsByInnKpp(string inn, string kpp);

        /// <summary>
        /// Получить контакт с контрагентом.
        /// </summary>
        /// <param name="boxId">ИД ящика нашей организации.</param>
        /// <param name="ourOrganizationId">Ид нашей организации.</param>
        /// <param name="counteragentId">Ид контрагента на сервисе обмена.</param>
        /// <returns>Контакт с контрагентом.</returns>
        IContact GetContact(string boxId, string ourOrganizationId, string counteragentId);

        ISubscriber GetCounteragent(string boxId, string organizationId);

        /// <summary>
        /// Получить актуальный статус контрагента.
        /// </summary>
        /// <param name="ourOrganizationId">ИД нашей организации в сервисе обмена. Должна соответствовать организации, к которой относится текущий пользователь.</param>
        /// <param name="counteragentId">Ид контрагента на сервисе обмена.</param>
        /// <returns>Статус.</returns>
        ContactStatus GetActualCounteragentStatus(string ourOrganizationId, string counteragentId);

        /// <summary>
        /// Получить тело документа из системы обмена в виде потока.
        /// </summary>
        /// <param name="boxId">ИД ящика.</param>
        /// <param name="docId">ИД документа в системе обмена.</param>
        /// <param name="messageId">ИД сообщения в системе обмена.</param>
        /// <param name="stream">Поток.</param>
        /// <remarks>messageId используется пока только в Диадок.</remarks>
        /// <remarks>Позиция в потоке при записи не сбрасывается: содержимое документа записывается в текущую позицию.</remarks>
        void GetDocumentContent(string boxId, string docId, string messageId, Stream stream);

        /// <summary>
        /// Получить печатную форму документа с сервиса обмена.
        /// </summary>
        /// <param name="boxId">ИД абонентского ящика.</param>
        /// <param name="messageId">ИД сообщения в системе обмена.</param>
        /// <param name="documentId">ИД документа в системе обмена.</param>
        /// <returns>Массив байт, содержащий ПДФ версию печатной формы документа.</returns>
        byte[] GetDocumentPrintedForm(string boxId, string messageId, string documentId);

        #region Устаревшие свойства
        /*
        /// <summary>
        /// Сгенерировать извещение о получении документа.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="documentId">Ид документа, на который нужно сгенерировать ИОП.</param>
        /// <param name="certThumbprint">Отпечаток сертификата.</param>
        /// <returns>Извещение о получении документа.</returns>
        IReglamentDocument GenerateDeliveryConfirmation(string ourBoxId, string documentId, string certThumbprint);

        /// <summary>
        /// Сгенерировать извещение о получении документа.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="documentId">Ид документа, на который нужно сгенерировать ИОП.</param>
        /// <param name="cert">Сертификат.</param>
        /// <returns>Извещение о получении документа.</returns>
        IReglamentDocument GenerateDeliveryConfirmation(string ourBoxId, string documentId, byte[] cert);

        /// <summary>
        /// Сгенерировать извещение о получении документа.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="documentId">Ид документа, на который нужно сгенерировать ИОП.</param>
        /// <param name="certThumbprint">Отпечаток сертификата.</param>
        /// <param name="rootServiceEntityId">ИД корневой сущности.</param>
        /// <param name="messageId">ИД сообщения.</param>     
        /// <returns>Извещение о получении документа.</returns>
        IReglamentDocument GenerateDeliveryConfirmation(string ourBoxId, string documentId, string certThumbprint, string rootServiceEntityId, string messageId = null);

        /// <summary>
        /// Сгенерировать извещение о получении документа.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="documentId">Ид документа, на который нужно сгенерировать ИОП.</param>
        /// <param name="cert">Сертификат.</param>
        /// <param name="rootServiceEntityId">ИД корневой сущности.</param>
        /// <param name="messageId">ИД сообщения.</param>     
        /// <returns>Извещение о получении документа.</returns>
        IReglamentDocument GenerateDeliveryConfirmation(string ourBoxId, string documentId, byte[] cert, string rootServiceEntityId, string messageId = null);

        /// <summary>
        /// Получить очередные служебные документы для отправки в систему обмена.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="document">Основной документ.</param>
        /// <param name="signerInfo">Информация о подписанте.</param>
        /// <param name="isDocflowFinished">Признак завершенности документооборота.</param>
        /// <returns>Очередные служебные документы.</returns>
        List<IReglamentDocument> GetNextReglamentDocuments(string ourBoxId, IDocument document, SignerInfo signerInfo, out bool isDocflowFinished);

        /// <summary>
        /// Сгенерировать извещение о получении для регламента СФ.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="parentDocumentId">Ид документа, на который нужно сгенерировать ИОП.</param>
        /// <param name="certThumbprint">Отпечаток сертификата.</param>
        /// <param name="rootServiceEntityId">ИД корневой сущности.</param>
        /// <param name="messageId">ИД сообщения.</param>
        /// <returns>Список извещений о получении регламента СФ.</returns>
        /// <remarks>Параметр messageId пока нужен только для Диадок.</remarks>
        IReglamentDocument GenerateInvoiceDeliveryConfirmation(string ourBoxId, string parentDocumentId, string certThumbprint, string rootServiceEntityId, string messageId = null);

        /// <summary>
        /// Сгенерировать извещение о получении для регламента СФ.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="parentDocumentId">Ид документа, на который нужно сгенерировать ИОП.</param>
        /// <param name="cert">Сертификат.</param>
        /// <param name="rootServiceEntityId">ИД корневой сущности.</param>
        /// <param name="messageId">ИД сообщения.</param>
        /// <returns>Список извещений о получении регламента СФ.</returns>
        /// <remarks>Параметр messageId пока нужен только для Диадок.</remarks>
        IReglamentDocument GenerateInvoiceDeliveryConfirmation(string ourBoxId, string parentDocumentId, byte[] cert, string rootServiceEntityId, string messageId = null);

        /// <summary>
        /// Сгенерировать уведомление об уточнении документа.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="document">Документ, на который нужно сгенерировать УОУ.</param>
        /// <param name="certThumbprint">Отпечаток сертификата.</param>
        /// <param name="comment">Комментарий к отказу.</param>
        /// <returns>Уведомление об уточнении документа.</returns>
        IReglamentDocument GenerateAmendmentRequest(string ourBoxId, IDocument document, string certThumbprint, string comment);

        /// <summary>
        /// Сгенерировать уведомление об уточнении на СФ.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="invoice">СФ, на который нужно сгенерировать УОУ.</param>
        /// <param name="certThumbprint">Отпечаток сертификата.</param>
        /// <param name="comment">Комментарий к УОУ.</param>
        /// <returns>УОУ на СФ.</returns>
        IReglamentDocument GenerateInvoiceAmendmentRequest(string ourBoxId, IDocument invoice, string certThumbprint, string comment);

        /// <summary>
        /// Сгенерировать уведомление об уточнении на СФ.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="invoice">СФ, на который нужно сгенерировать УОУ.</param>
        /// <param name="cert">Сертификат.</param>
        /// <param name="comment">Комментарий к УОУ.</param>
        /// <returns>УОУ на СФ.</returns>
        IReglamentDocument GenerateInvoiceAmendmentRequest(string ourBoxId, IDocument invoice, byte[] cert, string comment);

        /// <summary>
        /// Сгенерировать уведомления об уточнении на СФ.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="invoices">СФ, на которые нужно сгенерировать УОУ.</param>
        /// <param name="certThumbprint">Отпечаток сертификата.</param>
        /// <param name="comment">Комментарий к УОУ.</param>
        /// <returns>УОУ на СФ.</returns>
        List<IReglamentDocument> GenerateInvoiceAmendmentRequestsForPackage(string ourBoxId, List<IDocument> invoices, string certThumbprint, string comment);

        /// <summary>
        /// Сгенерировать уведомления об уточнении на СФ.
        /// </summary>
        /// <param name="ourBoxId">Ящик текущей организации.</param>
        /// <param name="invoices">СФ, на которые нужно сгенерировать УОУ.</param>
        /// <param name="cert">Cертификат.</param>
        /// <param name="comment">Комментарий к УОУ.</param>
        /// <returns>УОУ на СФ.</returns>
        List<IReglamentDocument> GenerateInvoiceAmendmentRequestsForPackage(string ourBoxId, List<IDocument> invoices, byte[] cert, string comment);

        /// <summary>
        /// Получить сообщение от сервиса.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="messageId">ИД сообщения.</param>
        /// <returns>Cообщение.</returns>
        IMessage GetMessage(ISubscriber ourSubscriber, string messageId);

        /// <summary>
        /// Получить сообщение от сервиса для перезагрузки.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="messageId">ИД сообщения.</param>
        /// <returns>Cообщение.</returns>
        IMessage GetMessageToReload(ISubscriber ourSubscriber, string messageId);

        /// <summary>
        /// Отправить сообщение в сервис.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="message">Сообщение.</param>
        /// <returns>Cообщение.</returns>
        SentMessage SendMessage(ISubscriber ourSubscriber, IMessage message);

        /// <summary>
        /// Получить события сервиса
        /// </summary>
        /// <param name="ourSubscriberId">ИД нашего абонента на сервисе в виде GUID </param>
        /// <param name="startingDate">Дата начала загрузки событий</param>
        /// <param name="lastProcessedEventId">ИД последнего обработанного</param>
        /// <param name="lastEventId">ИД последнего события в пачке событий, полученных с сервиса </param>
        /// <returns>Список событий</returns>
        List<IEvent> ReceiveEventsFromService(Guid ourSubscriberId, string lastProcessedEventId, DateTime? startingDate, out string lastEventId);

        /// <summary>
        /// Подготовить ответное сообщение к отправке.
        /// </summary>
        /// <param name="ourSubscriber">Абонент нашей организации в системе обмена.</param>
        /// <param name="message">Ответное сообщение.</param>
        /// <param name="signerInfo">Информация о подписанте, которая будет использоваться при генерации необходимых служебных документов.</param>
        /// <returns>Перечень документов, которые необходимо подписать перед отправкой.</returns>
        IEnumerable<IBaseDocument> PrepareReplyMessage(ISubscriber ourSubscriber, IMessage message, SignerInfo signerInfo);

        /// <summary>
        /// Получить сообщения от сервиса.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="lastReceivedMessageId">ИД последнего полученного входящего сообщения.</param>
        /// <param name="lastSentMessageId">ИД последнего полученного исходящего сообщения.</param>
        /// <param name="startingDate">Начальная дата загрузки (локальное время).</param>
        /// <returns>Список полученных сообщений.</returns>
        [Obsolete]
        List<IMessage> ReceiveMessagesFromService(ISubscriber ourSubscriber, string lastReceivedMessageId, string lastSentMessageId, DateTime? startingDate = null);

        /// <summary>
        /// Получить сообщения от сервиса.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="lastReceivedMessageId">ИД последнего полученного входящего сообщения.</param>
        /// <param name="lastSentMessageId">ИД последнего полученного исходящего сообщения.</param>
        /// <param name="startingDate">Начальная дата загрузки (локальное время).</param>
        /// <param name="lastIncomeEventId">ИД последнего входящего события.</param>
        /// <param name="lastOutcomeEventId">ИД последнего исходящего события.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список полученных сообщений.</returns>
        /// <exception cref="T:System.OperationCanceledException" />
        List<IMessage> ReceiveMessagesFromService(ISubscriber ourSubscriber, string lastReceivedMessageId, string lastSentMessageId, DateTime? startingDate, out string lastIncomeEventId, out string lastOutcomeEventId, CancellationToken cancellationToken);

        /// <summary>
        /// Сгенерировать запрос на аннулирование.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="documentId">ИД документа, который нужно аннулировать.</param>
        /// <param name="comment">Комментарий.</param>
        /// <param name="messageId">ИД сообщения на сервисе.</param>
        /// <param name="certificateThumbprint">Отпечаток сертификата.</param>
        /// <returns>Документ с аннулированием.</returns>
        /// <remarks>Параметры messageId и certificateThumbprint пока нужны только для Диадок.</remarks>
        Document GenerateRevocationOffer(ISubscriber ourSubscriber, string documentId, string comment, string messageId = null, string certificateThumbprint = null);

        /// <summary>
        /// Сгенерировать запрос на аннулирование.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="documentId">ИД документа, который нужно аннулировать.</param>
        /// <param name="comment">Комментарий.</param>
        /// <param name="signerInfo">Информация о подписанте.</param>
        /// <param name="messageId">ИД сообщения на сервисе.</param>
        /// <returns>Документ с аннулированием.</returns>
        /// <remarks>Параметры messageId и certificateThumbprint пока нужны только для Диадок.</remarks>
        Document GenerateRevocationOffer(ISubscriber ourSubscriber, string documentId, string comment, SignerInfo signerInfo, string messageId = null);

        /// <summary>
        /// Отправить запрос на аннулирование.
        /// </summary>
        /// <param name="ourSubscriber">Контрагент нашей организации в системе обмена.</param>
        /// <param name="counteragent">Контрагент, которому нужно отправить запрос на аннулирование.</param>
        /// <param name="document">Документ с аннулированием.</param>
        /// <param name="signContent">Подпись.</param>
        /// <returns>Отрпавленное сообщение с аннулированием.</returns>
        SentMessage SendRevocationOffer(ISubscriber ourSubscriber, ISubscriber counteragent, IDocument document, byte[] signContent);

        /// <summary>
        /// Отправить запрос на аннулирование.
        /// </summary>
        /// <param name="message">Сообщение с запросом на аннулирование.</param>
        /// <returns>Отправленное сообщение с аннулированием.</returns>
        SentMessage SendRevocationOffer(IMessage message);

        /// <summary>
        /// Получить информацию о нашем абоненте на сервисе.
        /// </summary>
        /// <returns>Наш абонент.</returns>
        ISubscriber GetOurSubscriber();

        /// <summary>
        /// Получить список сертификатов пользователей нашей организации.
        /// </summary>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="organizationId">ИД организации.</param>
        /// <param name="thumbprints">Список отпечатков, которые необходимо проверить.</param>
        /// <returns>Список сертификатов.</returns>
        List<Certificate> GetOrganizationCertificates(string boxId, string organizationId, List<byte[]> thumbprints);

        /// <summary>
        /// Проверить возможность/необходимость отправки извещения о получении для документа.
        /// </summary>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="documentId">ИД документа.</param>
        /// <param name="messageId">ИД сообщения.</param>
        /// <returns>True, если на указанный документ можно отправить ИОП.</returns>
        /// <remarks>messageId требуется только для Диадок.</remarks>
        bool CanSendDeliveryConfirmation(string boxId, string documentId, string messageId = null);

        /// <summary>
        /// Проверить возможность/необходимость отправки подписи\титула_покупателя\отказа\УОУ для документа.
        /// </summary>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="documentId">ИД документа.</param>
        /// <param name="messageId">ИД сообщения.</param>
        /// <param name="buyerTitleId">ИД титула покупателя.</param>
        /// <returns>Допустимые варианты подписания \ отказа \ УОУ на документ.</returns>
        DocumentAllowedAnswer GetAllowedAnswers(string boxId, string documentId, string messageId, string buyerTitleId);

        /// <summary>
        /// Загрузить ИОП с сервиса обмена.
        /// </summary>
        /// <param name="ourBoxId">ИД ящика организации.</param>
        /// <param name="parentEntityId">ИД родительского документа (на который был сгенерирован ИОП).</param>
        /// <param name="primaryDocumentId">ИД основного документа (корневой документ. Например, в случае ИОП на ПДО СФ корневым будет являться СФ).</param>
        /// <param name="primaryDocumentMessageId">ИД сообщения, в котором пришел основной документ.</param>
        /// <returns>ИОП на документ, или null, если извещение не найдено.</returns>
        SignedDocument<IReglamentDocument> GetReceipt(string ourBoxId, string parentEntityId, string primaryDocumentId, string primaryDocumentMessageId);

        /// <summary>
        /// Сформировать титул покупателя для накладной торг-12.
        /// </summary>
        /// <param name="bt">Информация о покупателе.</param>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="sellerTitleMessageId">ИД сообщения с титулом продавца.</param>
        /// <param name="sellerAttachmentId">ИД документа с титулом продавца.</param>
        /// <returns>Титул покупателя в xml формате.</returns>
        FileFromService GenerateTorg12XmlForBuyer(BuyerTitle bt, string boxId, string sellerTitleMessageId, string sellerAttachmentId);

        /// <summary>
        /// Сформировать титул покупателя для накладной торг-12.
        /// </summary>
        /// <param name="bt">Информация о покупателе.</param>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="sellerTitleMessageId">ИД сообщения с титулом продавца.</param>
        /// <param name="sellerAttachmentId">ИД документа с титулом продавца.</param>
        /// <returns>Титул покупателя в xml формате.</returns>
        FileFromService GenerateActXmlForBuyer(BuyerTitle bt, string boxId, string sellerTitleMessageId, string sellerAttachmentId);

        /// <summary>
        /// Сформировать титул покупателя для УПД.
        /// </summary>
        /// <param name="bt">Информация о покупателе.</param>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="sellerTitleMessageId">ИД сообщения с титулом продавца.</param>
        /// <param name="sellerAttachmentId">ИД документа с титулом продавца.</param>
        /// <returns>Титул покупателя в xml формате.</returns>
        FileFromService GenerateUniversalTransferDocumentXmlForBuyer(BuyerTitle bt, string boxId, string sellerTitleMessageId, string sellerAttachmentId);

        /// <summary>
        /// Сформировать титул покупателя для УКД.
        /// </summary>
        /// <param name="bt">Информация о покупателе.</param>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="sellerTitleMessageId">ИД сообщения с титулом продавца.</param>
        /// <param name="sellerAttachmentId">ИД документа с титулом продавца.</param>
        /// <returns>Титул покупателя в xml формате.</returns>
        FileFromService GenerateUniversalTransferCorrectionDocumentXmlForBuyer(BuyerTitle bt, string boxId, string sellerTitleMessageId, string sellerAttachmentId);

        /// <summary>
        /// Сформировать титул покупателя для ДПРР.
        /// </summary>
        /// <param name="bt">Информация о покупателе.</param>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="sellerTitleMessageId">ИД сообщения с титулом продавца.</param>
        /// <param name="sellerAttachmentId">ИД документа с титулом продавца.</param>
        /// <returns>Титул покупателя в xml формате.</returns>
        FileFromService GenerateWorksTransferXmlForBuyer(BuyerTitle bt, string boxId, string sellerTitleMessageId, string sellerAttachmentId);

        /// <summary>
        /// Сформировать титул покупателя для ДПТ.
        /// </summary>
        /// <param name="bt">Информация о покупателе.</param>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="sellerTitleMessageId">ИД сообщения с титулом продавца.</param>
        /// <param name="sellerAttachmentId">ИД документа с титулом продавца.</param>
        /// <returns>Титул покупателя в xml формате.</returns>
        FileFromService GenerateGoodsTransferXmlForBuyer(BuyerTitle bt, string boxId, string sellerTitleMessageId, string sellerAttachmentId);
        */
        #endregion

        /// <summary>
        /// Получить ссылку на документ в вебе.
        /// </summary>
        /// <param name="boxId">ИД ящика организации.</param>
        /// <param name="messageId">ИД сообщения.</param>
        /// <param name="documentId">ИД документа.</param>
        /// <returns>Ссылка на открытие документа в вебе.</returns>
        Uri GetDocumentUri(string boxId, string messageId, string documentId);
    }


    /// <summary>
    /// Абонент в системе обмена
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// Информация об организации.
        /// </summary>
        //Organization Organization { get; }

        /// <summary>
        /// ИД ящика организации на сервисе обмена.
        /// </summary>
        string BoxId { get; }

        /// <summary>
        /// Коллекция подразделений абонента.
        /// </summary>
        //IList<Department> Departments { get; }

        /// <summary>
        /// Адрес головного подразделения
        /// </summary>
        //Department HeadDepartment { get; }

        /// <summary>
        /// Признак, что абонент подписал регламент ФНС для обмена счетами-фактурами.
        /// </summary>
        bool InvoiceReglamentAccepted { get; }

        /// <summary>
        /// ИД контрагента.
        /// </summary>
        string CounteragentId { get; set; }

        /// <summary>
        /// ИД участника документооборота СФ, зарегистрированный в ФНС.
        /// </summary>
        string FnsParticipantId { get; set; }

        /// <summary>
        /// ИД абонента в виде GUID.
        /// </summary>
        Guid SubscriberId { get; set; }
    }

    /// <summary>
    /// Организация-участник обмена электронными документами.
    /// </summary>
    public class Organization
    {
        /// <summary>
        /// Наименование организации.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Юридическое наименование организации.
        /// </summary>
        public string LegalName { get; set; }

        /// <summary>
        /// Имя (для ФЛ).
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Фамилия (для ФЛ).
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Отчество (для ФЛ).
        /// </summary>
        public string MiddleName { get; set; }

        /// <summary>
        /// ИНН организации.
        /// </summary>
        public string Inn { get; set; }

        /// <summary>
        /// КПП организации.
        /// </summary>
        public string Kpp { get; set; }

        /// <summary>
        /// ОГРН организации.
        /// </summary>
        public string Ogrn { get; set; }

        /// <summary>
        /// ИД организации.
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// ИД участника документооборота СФ, зарегистрированный в ФНС.
        /// </summary>
        public string FnsParticipantId { get; set; }

        /// <summary>
        /// БИК.
        /// </summary>
        public string Bik { get; set; }

        /// <summary>
        /// Расчетный счет.
        /// </summary>
        public string CurrentAccount { get; set; }

        /// <summary>
        /// Телефон.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Факс.
        /// </summary>
        public string Fax { get; set; }

        /// <summary>
        /// Адрес абонентского ящика. 
        /// </summary>
        public string BoxId { get; set; }

        /// <summary>
        /// Юридический адрес.
        /// </summary>
        //public OrganizationAddress LegalAddress { get; set; }

        /// <summary>
        /// Почтовый адрес.
        /// </summary>
        //public OrganizationAddress MailAddress { get; set; }

        /// <summary>
        /// Адрес регистрации.
        /// </summary>
        //public OrganizationAddress RegistrationAddress { get; set; }

        /// <summary>
        /// Тип организации.
        /// </summary>
        //public OrganizationType OrganizationType { get; set; }

        /// <summary>
        /// Признак роуминга.
        /// </summary>
        public bool IsRoaming { get; set; }

        /// <summary>
        /// Название сервиса обмена.
        /// </summary>
        public string ExchangeServiceName { get; set; }

        /// <summary>
        /// Код оператора.
        /// </summary>
        public string OperatorCode { get; set; }
    }

    /// <summary>
    /// Организация-участник обмена электронными документами.
    /// </summary>
    public class Participatin
    {
        /// <summary>
        /// Количество организаций.
        /// </summary>
        [JsonProperty("count")]
        public int count { get; set; }

        /// <summary>
        /// Тело ответа.
        /// </summary>
        [JsonProperty("data")]
        public List<ContactAdressBook> data { get; set; }



        /// <summary>
        /// Получение адресной книги
        /// </summary>
        /// <param name="organizationId">ИД организации</param>
        /// <param name="AuthenticationToken">Токен Сессии</param>
        /// <param name="lastSync">Дата последней авторизации</param>
        /// <returns>List контрагентов</returns>
        public static List<ContactAdressBook> GetContactList(string organizationId, string AuthenticationToken, System.DateTime lastSync)
        {
            ServicePointManager.DefaultConnectionLimit = 20;
            string orgId = organizationId.Remove(0, 3);

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(String.Format("https://apiedo.etpgpb.ru/Invitations/{0}/GetAddressBook/table", orgId));

            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("SessionKey", AuthenticationToken);


            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                JObject jsonObject = new JObject();
                jsonObject.Add("beginDate", "2018-01-01 00-00-01");
                //lastSync.ToString("yyyy-MM-dd HH-mm-ss")
                streamWriter.Write(jsonObject);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string response;

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                response = streamReader.ReadToEnd();
            }


            Participatin Parti = JsonConvert.DeserializeObject<Participatin>(response);
            List<ContactAdressBook> result = Parti.data;

            return result;
        }
    }

    /// <summary>
    /// Организация-участник обмена электронными документами.
    /// </summary>
    public class Counterparty
    {
        /// <summary>
        /// ИД организации.
        /// </summary>
        [JsonProperty("id")]
        public string id { get; set; }

        /// <summary>
        /// Адрес абонентского ящика. 
        /// </summary>
        [JsonProperty("guid")]
        public string guid { get; set; }

        /// <summary>
        /// ИД отправителя. 
        /// </summary>
        [JsonProperty("sender")]
        public string sender { get; set; }

        /// <summary>
        /// ИД получателя. 
        /// </summary>
        [JsonProperty("receiver")]
        public string receiver { get; set; }

        /// <summary>
        /// Код оператора.
        /// </summary>
        [JsonProperty("sos")]
        public string sos { get; set; }

        /// <summary>
        /// ИНН организации.
        /// </summary>
        [JsonProperty("Inn")]
        public string Inn { get; set; }

        /// <summary>
        /// КПП организации.
        /// </summary>
        [JsonProperty("Kpp")]
        public string? Kpp { get; set; }

        /// <summary>
        /// Наименование организации.
        /// </summary>
        [JsonProperty("Name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Организация-участник обмена электронными документами.
    /// </summary>
    public class ContactAdressBook
    {
        /// <summary>
        /// Организация контрагента.
        /// </summary>        
        public Counterparty Organization { get; set; }

        /// <summary>
        /// Состояние обмена с контрагентом.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("updated_at")]
        public string StatusChangeDate { get; set; }

        /// <summary>
        /// Комментарий контрагента.
        /// </summary>
        [JsonProperty("message")]
        public string Comment { get; set; }

    }

    /// <summary>
    /// Статус связи с контрагентом.
    /// </summary>
    public enum ContactStatus
    {
        /// <summary>
        /// Отправка запроса.
        /// </summary>
        Sent,
        /// <summary>
        /// Подтверждение контрагентом.
        /// </summary>
        Accept,
        /// <summary>
        /// Отправка отказа.
        /// </summary>
        Reject
    }

    public static class Log
    {
        private static LogDelegate logDelegate;

        private static LogExceptionDelegate logExceptionDelegate;

        public static void Initialize(LogDelegate logDelegate, LogExceptionDelegate logExceptionDelegate)
        {
            Log.logDelegate = logDelegate;
            Log.logExceptionDelegate = logExceptionDelegate;
        }

        public static void Trace(string msg, params object[] args)
        {
            logDelegate(LogLevel.Trace, msg, args);
        }

        public static void Debug(string msg, params object[] args)
        {
            logDelegate(LogLevel.Debug, msg, args);
        }

        public static void Info(string msg, params object[] args)
        {
            logDelegate(LogLevel.Info, msg, args);
        }

        public static void Warn(string msg, params object[] args)
        {
            logDelegate(LogLevel.Warn, msg, args);
        }

        public static void Error(string msg, params object[] args)
        {
            logDelegate(LogLevel.Error, msg, args);
        }

        public static void Fatal(string msg, params object[] args)
        {
            logDelegate(LogLevel.Fatal, msg, args);
        }

        public static void Exception(Exception ex)
        {
            logExceptionDelegate("", ex);
        }
    }
    public delegate void LogDelegate(LogLevel level, string msg, params object[] args);

    public delegate void LogExceptionDelegate(string msg, Exception ex, params object[] args);
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    /// <summary>
    /// Настройки прокси.
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// Сервер.
        /// </summary>
        public Uri Server { get; private set; }

        /// <summary>
        /// Пользователь прокси.
        /// </summary>
        public string User { get; private set; }

        /// <summary>
        /// Пароль.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        public ProxySettings()
        {
        }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="server">Сервер.</param>
        /// <param name="user">Имя пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        public ProxySettings(Uri server, string user, string password)
        {
            Server = server;
            User = user;
            Password = password;
        }
    }

    /// <summary>
    /// Настройки сервиса: URL, прокси, временные папки.
    /// </summary>
    public class ServiceSettings
    {
        /// <summary>
        /// Url-адрес сервиса.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Url-адрес сервиса для построения ссылок на документы.
        /// </summary>
        public string HyperlinkUrl { get; set; }

        /// <summary>
        /// Прокси.
        /// </summary>
        public ProxySettings Proxy { get; set; }

        /// <summary>
        /// ИНН организации на сервисе.
        /// </summary>
        public string OurOrganizationInn { get; set; }

        /// <summary>
        /// КПП организации на сервисе.
        /// </summary>
        public string OurOrganizationKpp { get; set; }

        /// <summary>
        /// ИД ящика организации на сервисе.
        /// </summary>
        public string OurOrganizationBoxId { get; set; }

        /// <summary>
        /// Код оператора.
        /// </summary>
        public string OperatorCode { get; set; }
    }


    /// <summary>
    /// Система обмена.
    /// </summary>
    public enum ExchangeSystem
    {
        ETPGPB
    }
}
