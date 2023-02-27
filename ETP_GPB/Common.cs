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
	public static class ClientBase
    {
		/// <summary>
		/// Получить список контактов организации.
		/// Список контактов включает в себя все контакты организации, в том числе те, с которыми обмен запрещен.
		/// </summary>
		/// <param name="organizationId">ИД организации</param>
		/// <param name="AuthenticationToken">Токен Сессии</param>
		/// <param name="lastSync">Дата последней авторизации</param>
		/// <returns>Список контрагентов организации.</returns>
		public static Organization GetContacts(string organizationId, string AuthenticationToken, System.DateTime lastSync)
		{
			Organization adressBook = Organization.GetAddressBook(organizationId, AuthenticationToken, lastSync);
			return adressBook;
			
		}

		/*/// <summary>
		/// Создать экземпляр клиента.
		/// </summary>
		/// <param name="clientSystem">Система обмена.</param>
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
				throw new ExchangeSystemConnectorResolveException($"Не найден коннектор для системы обмена {clientSystem}");
			}
			serviceClient.Initialize(settings, authTokenProvider, UpdateToken, connectorSetting);
		}*/
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

	}

	/// <summary>
	/// Организация-участник обмена электронными документами.
	/// </summary>
	public class Organization
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
		public static Organization GetAddressBook(string organizationId, string AuthenticationToken, System.DateTime lastSync)
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
			List<Organization> result = new List<Organization>();

			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				response = streamReader.ReadToEnd();
			}
						

			Organization organizationList = JsonConvert.DeserializeObject<Organization>(response);

			return organizationList;
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

		/// <summary>
		/// Статус документооборота.
		/// </summary>
		[JsonProperty("message")]
		public string message { get; set; }
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
}
