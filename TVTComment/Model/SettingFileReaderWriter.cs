using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    class SettingFileReaderWriter<SettingT> where SettingT : new()
    {
        class TimeSpanConverter : JsonConverter<TimeSpan>
        {
            public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var value = reader.GetString();
                return TimeSpan.Parse(value);
            }

            public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }

        private readonly JsonSerializerOptions jsonSerializerOptions;

        public string FilePath { get; }
        public SettingFileReaderWriter(string filepath, bool appendFileExtension)
        {
            this.FilePath = filepath;
            if (appendFileExtension)
                this.FilePath = this.FilePath + ".json";

            this.jsonSerializerOptions = new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals |
                                 JsonNumberHandling.AllowReadingFromString
            };
            this.jsonSerializerOptions.Converters.Add(new TimeSpanConverter());
        }

        public async Task<SettingT> Read()
        {
            StreamReader reader;
            try
            {
                reader = new StreamReader(this.FilePath, Encoding.UTF8);
            }
            catch(Exception e) when(e is DirectoryNotFoundException || e is FileNotFoundException)
            {
                return new SettingT();
            }
            try
            {
                return JsonSerializer.Deserialize<SettingT>(await reader.ReadToEndAsync(), jsonSerializerOptions);
            }
            catch(JsonException e)
            {
                throw new FormatException(null, e);
            }
            finally
            {
                reader.Dispose();
            }
        }

        public async Task Write(SettingT setting)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath));

            using var writer = new StreamWriter(this.FilePath, false, Encoding.UTF8);
            await writer.WriteAsync(JsonSerializer.Serialize(setting, jsonSerializerOptions));
        }
    }
}
