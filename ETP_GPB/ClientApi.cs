using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;

namespace ETPGPB
{
	public class Client : ClientBase
	{
		protected override IExchangeServiceConnector ResolveConnector(ExchangeSystem clientSystem)
		{
			return new ETP();
		}

		private static ConcurrentDictionary<string, Client> cache = new ConcurrentDictionary<string, Client>();


		/// <summary>
		/// Создать экземпляр клиента.
		/// </summary>
		/// <param name="clientSystem">Система обмена.</param>
		/// <param name="settings">Настройки системы обмена.</param>
		/// <param name="tokenProvider">Провайдер токена аутентификации для сервиса обмена.</param>
		/// <param name="connectorSetting">Расширенные настройки подключения к коннектору.</param>
		public Client(ExchangeSystem clientSystem, ServiceSettings settings, IAuthTokenProvider tokenProvider = null, ConnectorSettings connectorSetting = null)
			: base(clientSystem, settings, tokenProvider, connectorSetting)
		{
		}
	}
}
