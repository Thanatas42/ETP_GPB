using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace ETPGPB
{
    public abstract class ClientBase
    {
		/// <summary>
		/// Получить список контактов организации.
		/// Список контактов включает в себя все контакты организации, в том числе те, с которыми обмен запрещен.
		/// </summary>
		/// <param name="changedAfterDate">Дата последнего изменения контактов.
		/// Если указана, будут выбраны только те контакты, которые были измененны после заданной даты.
		/// При сравнении дата будет преобразовываться в формат UTC.</param>
		/// <param name="cancellationToken">Токен для отмены операции.</param>
		/// <returns>Список контактов организации.</returns>
		public static int GetContacts()
		{
			int asd = 123;

			return asd;			
		}

		/// <summary>
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
		}


		/// <summary>
		/// Коннектор, через который работаем
		/// </summary>
		protected IExchangeServiceConnector serviceClient;
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

	// NpoComputer.DCX.Common.ContactStatus
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
