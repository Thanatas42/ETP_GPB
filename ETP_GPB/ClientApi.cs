﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ETPGPB
{
    public class Client : ClientBase
    {

		/// <summary>
		/// Создать экземпляр клиента.
		/// </summary>
		/// <param name="clientSystem">Система обмена.</param>
		/// <param name="settings">Настройки системы обмена.</param>
		/// <param name="tokenProvider">Провайдер токена аутентификации для сервиса обмена.</param>
		/// <param name="connectorSetting">Расширенные настройки подключения к коннектору.</param>
		public Client()
			: base(clientSystem, settings, tokenProvider, connectorSetting)
		{

		}

		public static int GetContacts()
        {
            int asd = 123;
            return asd;
        }
    }
}