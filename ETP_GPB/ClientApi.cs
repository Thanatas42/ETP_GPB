using System;
using System.Collections.Generic;
using ETPlibrary.ETPGPB.Common;

namespace ETPlibrary.ETPGPB.ClientApi
{
    public static class Client
    {
		public static List<ContactAdressBook> GetContacts(string organizationId, string AuthenticationToken, System.DateTime lastSync)
        {
			return ClientBase.GetContacts(organizationId, AuthenticationToken, lastSync);
        }

        public static List<Message> GetUnreadDocuments(string AuthenticationToken)
        {
            return ClientBase.GetUnreadDocuments(AuthenticationToken);
        }

    }

}
