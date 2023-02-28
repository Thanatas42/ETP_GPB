using System;
using System.Collections.Generic;
using System.Text;
using ETPGPB;

namespace ETPGPB
{
	public class ETP : IExchangeServiceConnector
	{
		public void Initialize(ServiceSettings serviceSettings, IAuthTokenProvider authTokenProvider, Func<bool> tokenExpiredHandler, ConnectorSettings connectorSetting = null)
		{
			if (serviceSettings == null)
			{
				throw new ArgumentNullException("serviceSettings");
			}
			/*tokenProvider = authTokenProvider ?? throw new ArgumentNullException("authTokenProvider");
			onTokenExpired = tokenExpiredHandler;
			ourSubscriberInn = serviceSettings.OurOrganizationInn;
			ourSubscriberKpp = serviceSettings.OurOrganizationKpp;
			ourSubscriberOperatorCode = serviceSettings.OperatorCode;
			ourSubscriberBoxId = serviceSettings.OurOrganizationBoxId;
			BasicHttpBinding val = new BasicHttpBinding((BasicHttpSecurityMode)1);
			((HttpBindingBase)val).set_MaxBufferPoolSize(200000000L);
			((HttpBindingBase)val).set_MaxBufferSize(200000000);
			((HttpBindingBase)val).set_MaxReceivedMessageSize(200000000L);
			((HttpBindingBase)val).set_ReaderQuotas(new XmlDictionaryReaderQuotas
			{
				MaxDepth = 32,
				MaxArrayLength = 200000000,
				MaxStringContentLength = 200000000
			});
			((Binding)val).set_SendTimeout(new TimeSpan(0, 10, 0));
			BasicHttpBinding binding = val;
			serviceClient = new ExchangeServiceClient((Binding)(object)binding, new EndpointAddress(serviceSettings.ServiceUrl));
			string eventServiceName = GetServiceName(serviceSettings.ServiceUrl, "EventServiceV1.svc");
			serviceEventClient = new EventServiceV1Client((Binding)(object)binding, new EndpointAddress(eventServiceName));
			hyperlinkUrl = serviceSettings.HyperlinkUrl;
			ApplyProxySettings(serviceSettings.Proxy);
			ConnectorSetting = connectorSetting;*/
		}
	}

}
