using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ETPGPB
{
	public abstract class ClientBase
	{

		/// <summary>
		/// Объект для доступа к токену авторизации на сервисе обмена.
		/// </summary>
		protected readonly IAuthTokenProvider authTokenProvider;

		private ISubscriber ourSubscriber;

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
		public static List<Counterparty> GetContacts(string organizationId, string AuthenticationToken, System.DateTime lastSync)
		{
			List<Counterparty> adressBook = Participatin.GetContactList(organizationId, AuthenticationToken, lastSync);
			return adressBook;
			
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
		/// Создать экземпляр клиента.
		/// </summary>
		/// <param name="settings">Настройки системы обмена.</param>
		/// <param name="tokenProvider">Провайдер токена аутентификации для сервиса обмена.</param>
		/// <param name="connectorSetting">Расширенные настройки подключения к коннектору.</param>
		protected ClientBase(ExchangeSystem clientSystem, ServiceSettings settings, IAuthTokenProvider tokenProvider, ConnectorSettings connectorSetting = null)
		{
			


			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}
			if (settings.Proxy?.Server == null)
			{
				InitializeDefaultProxy();
			}
			authTokenProvider = tokenProvider ?? new DefaultAuthTokenProvider();
			serviceClient = ResolveConnector(clientSystem);
			if (serviceClient == null)
			{
				//throw new ExchangeSystemConnectorResolveException($"Не найден коннектор для системы обмена {clientSystem}");
			}
			serviceClient.Initialize(settings, authTokenProvider, UpdateToken, connectorSetting);
		}

		/// <summary>
		/// Загрузить из СОД информацию о Нашем абоненте для использования внутри класса
		/// </summary>
		private void GetOurSubscriber()
		{
			ourSubscriber = serviceClient.GetOurSubscriber();
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
		/// Подключиться к СОД по паролю.
		/// </summary>
		/// <param name="login">Логин пользователя в СОД.</param>
		/// <param name="password">Пароль пользователя в СОД.</param>
		public void Login(string login, string password)
		{
			string token;
			try
			{
				token = serviceClient.AuthenticateByPassword(login, password);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Ошибка аутентификации. Проверьте правильность логина и пароля.", ex);
			}
			authTokenProvider.AuthenticationToken = token;
			GetOurSubscriber();
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
		//public Organization Organization { get; set; }

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
		//Organization Organization { get; }

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
		/// Выполнить аутентификацию на сервисе обмена по логину и паролю.
		/// </summary>
		/// <param name="login">Логин пользователя.</param>
		/// <param name="password">Пароль пользователя.</param>
		/// <returns>Токен авторизации.</returns>
		/// <exception cref="T:NpoComputer.DCX.Common.Exceptions.AuthenticationException">Некорректный логин/пароль</exception>
		string AuthenticateByPassword(string login, string password);

		ISubscriber GetOurSubscriber();

		/// <summary>
		/// Инициализировать коннектор.
		/// </summary>
		/// <param name="serviceSettings">Настройки коннектора.</param>
		/// <param name="authTokenProvider">Провайдер токена аутентификации.</param>
		/// <param name="tokenExpiredHandler">Обработчик события на истечение срока действия токена аутентификации.</param>
		/// <param name="connectorSetting">Расширенные настройки подключения к коннектору.</param>
		void Initialize(ServiceSettings serviceSettings, IAuthTokenProvider authTokenProvider, Func<bool> tokenExpiredHandler, ConnectorSettings connectorSetting);
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
		public List<Counterparty> data { get; set; }

		/// <summary>
		/// Получение адресной книги
		/// </summary>
		/// <param name="organizationId">ИД организации</param>
		/// <param name="AuthenticationToken">Токен Сессии</param>
		/// <param name="lastSync">Дата последней авторизации</param>
		/// <returns>List контрагентов</returns>
		public static List<Counterparty> GetContactList(string organizationId, string AuthenticationToken, System.DateTime lastSync)
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
			List<Counterparty> result = Parti.data;

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
		public string Kpp { get; set; }

		/// <summary>
		/// Наименование организации.
		/// </summary>
		[JsonProperty("Name")]
		public string Name { get; set; }

		/// <summary>
		/// Статус документооборота.
		/// </summary>
		[JsonProperty("status")]
		public string status { get; set; }

		
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
		Reject,
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
