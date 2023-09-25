using System;

namespace ETPlibrary.ETPGPB.JSONLibrary.JsonParser
{
	// JSONLibrary.JsonParser
	public static class JsonParser
	{
		public static string GetJsonProperty(string source, string propertyName, int breakets = -1)
		{
			string formattedPropName = "\"" + propertyName + "\"";
			string sub = source.Substring(source.IndexOf(formattedPropName));
			string text = source.Substring(0, source.Length - sub.Length);
			foreach (char num in text)
			{
				if (num == '{')
				{
					breakets++;
				}
				if (num == '}')
				{
					breakets--;
				}
			}
			if (breakets == 0)
			{
				return CutPropertyString(sub);
			}
			return GetJsonProperty(sub.Substring(formattedPropName.Length), propertyName, breakets);
		}

		public static string GetJsonProperty(string source, string[] propertyNames)
		{
			string finalProperty = source;
			foreach (string name in propertyNames)
			{
				finalProperty = GetJsonProperty(finalProperty, name);
			}
			return finalProperty;
		}

		private static string CutPropertyString(string source)
		{
			int colonIndex = source.IndexOf(":");
			if (source[colonIndex + 1] == '{')
			{
				return CutPropWithBreakets(source, '{', '}');
			}
			if (source[colonIndex + 1] == '[')
			{
				return CutPropWithBreakets(source, '[', ']');
			}
			return CutSingleProperty(source);
		}

		private static string CutSingleProperty(string source)
		{
			int colonIndex = source.IndexOf(":");
			string propBody = source.Substring(colonIndex);
			return source.Substring(0, source.IndexOf(propBody)) + GetSinglePropertyValue(propBody);
		}

		private static string CutPropWithBreakets(string source, char openBreaket, char closeBreaket)
		{
			string blockBody = source.Substring(source.IndexOf(openBreaket));
			string blockHeader = source.Substring(0, source.IndexOf(blockBody));
			int breakets = 0;
			string result = string.Empty;
			string text = blockBody;
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				result += c;
				if (c == openBreaket)
				{
					breakets++;
				}
				if (c == closeBreaket)
				{
					breakets--;
				}
				if (breakets == 0)
				{
					break;
				}
			}
			return blockHeader + result;
		}

		public static string GetJsonPropertyValue(string property)
		{
			int colonIndex = property.IndexOf(":");
			string propBody = property.Substring(colonIndex + 1);
			propBody = propBody.TrimStart('"');
			if (propBody[propBody.Length - 1] == '"')
			{
				propBody = propBody.Substring(0, propBody.Length - 1);
			}
			return propBody;
		}

		public static string DisplayJsonPropertyValue(string property)
		{
			string valueToDisplay = GetJsonPropertyValue(property);
			if (valueToDisplay.Contains("\\\""))
			{
				valueToDisplay = valueToDisplay.Replace("\\\"", "\"");
			}
			return valueToDisplay;
		}

		public static string GetJsonListElement(string property, int index)
		{
		    string propBody = string.Empty;

		    if (property.Trim()[0] == '[')
			propBody = property.TrimStart('[').TrimEnd(']');
		    else if (property[property.IndexOf(":") + 1] == '[')
			propBody = property.Substring(property.IndexOf(":") + 1).TrimStart('[').TrimEnd(']');
		    else
			throw new Exception("Не является свойством-коллекцией");

		    int startIndex = 0;
		    string result = string.Empty;
		    for (int i = 0; i <= index; i++)
		    {
			propBody = propBody.Substring(startIndex).TrimStart();
			if (string.IsNullOrEmpty(propBody))
			    throw new IndexOutOfRangeException();
			else
			{
			    if (propBody[0] == '{')
				result = CutPropWithBreakets(propBody, '{', '}');
			    else if (propBody[0] == '[')
				result = CutPropWithBreakets(propBody, '[', ']');
			    else
				result = GetSinglePropertyValue(propBody);

			    if (i == index)
				result = result.Trim(',');
			    else
			    {
				startIndex = result.Length + 1;
				result = string.Empty;
			    }
			}
		    }
		    return result;
		}

		public static int GetJsonListCount(string property)
		{
		    int count = 0;
		    string element = GetJsonListElement(property, count);

		    while (element != String.Empty)
		    {
			try
			{
			    count++;
			    element = GetJsonListElement(property, count);
			}
			catch
			{
			    break;
			}
		    }
		    return count;
		}

		public static string GetSinglePropertyValue(string value)
		{
			string result = string.Empty;
			if (value[0] == '"')
			{
				int quotes = 0;
				string text = value;
				for (int i = 0; i < text.Length; i++)
				{
					char c2 = text[i];
					result += c2;
					if (c2 == '"')
					{
						quotes++;
					}
					if (quotes == 2)
					{
						break;
					}
				}
			}
			else
			{
				string text = value;
				for (int i = 0; i < text.Length; i++)
				{
					char c = text[i];
					if (c == ',' || c == '}' || c == ']')
					{
						break;
					}
					result += c;
				}
			}
			return result;
		}

		public static string RemoveJsonProperty(string source, string propertyName)
		{
			string propertyString = GetJsonProperty(source, propertyName);
			int propertyIndex = source.IndexOf(propertyString);
			string text = source.Substring(0, propertyIndex);
			string right = source.Substring(propertyIndex + propertyString.Length);
			string result = text + right;
			if (source[propertyIndex - 1] != ',')
			{
				return result.Remove(propertyIndex, 1);
			}
			return result.Remove(propertyIndex - 1, 1);
		}

		public static string SetJsonProperty(string source, string propertyName, string value, bool quotes)
		{
			int propertyIndex = source.IndexOf(GetJsonProperty(source, propertyName));
			string cutSource = RemoveJsonProperty(source, propertyName);
			if (quotes)
			{
				value = "\"" + value + "\"";
			}
			string propertyToInsert = "\"" + propertyName + "\":" + value;
			string left = string.Empty;
			string right = string.Empty;
			if (cutSource[propertyIndex - 1] == '{' || cutSource[propertyIndex - 1] == '[')
			{
				left = cutSource.Substring(0, propertyIndex);
				right = cutSource.Substring(propertyIndex);
			}
			else
			{
				left = cutSource.Substring(0, propertyIndex - 1);
				right = cutSource.Substring(propertyIndex - 1);
			}
			source = ((cutSource[propertyIndex - 1] != '{' && cutSource[propertyIndex - 1] != '[') ? (left + "," + propertyToInsert + right) : (left + propertyToInsert + "," + right));
			return source;
		}

		public static string SetJsonListElement(string source, string value, int index)
		{
			int placeToInsert = source.IndexOf(GetJsonListElement(source, index));
			int gap = GetJsonListElement(source, index).Length;
			string text = source.Substring(0, placeToInsert);
			string right = source.Substring(placeToInsert + gap);
			return text + value + right;
		}

		public static string AddJsonListElement(string jsonList, string elementToAdd)
		{
			int lastBreaketIndex = jsonList.LastIndexOf("]");
			if (lastBreaketIndex == -1)
			{
				return jsonList;
			}
			int count = GetJsonListCount(jsonList);
			if (count == 0)
			{
				jsonList = jsonList.Insert(lastBreaketIndex, elementToAdd);
			}
			else if (count > 0)
			{
				jsonList = jsonList.Insert(lastBreaketIndex, "," + elementToAdd);
			}
			return jsonList;
		}
	}
}
