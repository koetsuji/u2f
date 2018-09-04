﻿using System;
using Newtonsoft.Json;

namespace MonoSign.U2F
{
	public class FidoKeyHandleConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, ((FidoKeyHandle)value).ToWebSafeBase64());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
		    return FidoKeyHandle.FromWebSafeBase64(reader.Value.ToString());
		}

	    public override bool CanConvert(Type objectType)
		{
			return typeof(FidoKeyHandle).IsAssignableFrom(objectType);
		}
	}
}
