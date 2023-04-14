using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;

namespace ETPGPB
{
	public static class Client
	{	
		public static List<ContactAdressBook> GetContacts(string organizationId, string AuthenticationToken, System.DateTime lastSync)
        {
			return ClientBase.GetContacts(organizationId, AuthenticationToken, lastSync);
        }

	}


}
