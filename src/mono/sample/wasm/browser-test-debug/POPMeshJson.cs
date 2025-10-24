// Copyright 2023 to current year. AVEVA Solutions Limited and its subsidiaries. All rights reserved.
//
// POPMeshImportLib json stuff
// 
// @author Giuseppe Donvito

using System;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Sample
{

    public class JsonConverterVector3 : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            double x = jObject["x"]?.Value<double>() ?? 0.0;
            double y = jObject["y"]?.Value<double>() ?? 0.0;
            double z = jObject["z"]?.Value<double>() ?? 0.0;

            return new double[] { x, y, z };
        }

        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            //return objectType == typeof(Vector3);
            return objectType == typeof(double[]);
        }
    }
    public class BoundingBox
    {
        public double[] Min { get; set; }
        public double[] Max { get; set; }

        public BoundingBox(double[] min, double[] max)
        {
            if (min == null || max == null)
                throw new ArgumentNullException("Min and Max arrays cannot be null.");
            if (min.Length != 3 || max.Length != 3)
                throw new ArgumentException("Min and Max arrays must have exactly 3 elements.");

            Min = min;
            Max = max;
        }
    }

    public class JsonConverterBoundingBox : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is BoundingBox)
            {
                var bbox = (BoundingBox)value;
                double[] min = new double[] { bbox.Min[0], bbox.Min[1], bbox.Min[2] };
                double[] max = new double[] { bbox.Max[0], bbox.Max[1], bbox.Max[2] };
                writer.WriteStartObject();
                writer.WritePropertyName("min");
                writer.WriteStartArray();
                writer.WriteValue(min[0]);
                writer.WriteValue(min[1]);
                writer.WriteValue(min[2]);
                writer.WriteEndArray();
                writer.WritePropertyName("max");
                writer.WriteStartArray();
                writer.WriteValue(max[0]);
                writer.WriteValue(max[1]);
                writer.WriteValue(max[2]);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            // Load JObject from stream
            JObject jObject = JObject.Load(reader);
            var tmin = jObject["min"];
            var tmax = jObject["max"];
            if (tmin == null || tmax == null || tmin.Count() < 3 || tmax.Count() < 3)
                return null;

            double[] min = tmin.ToObject<double[]>();
            double[] max = tmax.ToObject<double[]>();
            return new BoundingBox(min, max);
        }

        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BoundingBox);
        }
    }
}
