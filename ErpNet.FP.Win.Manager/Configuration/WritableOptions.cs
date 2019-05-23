using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace ErpNet.FP.Server.Configuration
{
    public interface IWritableOptions<out T> : IOptionsSnapshot<T> where T : class, new()
    {
        void Update(Action<T> applyChanges);
    }

    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IOptionsMonitor<T> options;
        private readonly string section;
        private readonly string file;

        public WritableOptions(
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            this.options = options;
            this.section = section;
            this.file = file;
        }

        public T Value => options.CurrentValue;
        public T Get(string name) => options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(file));
            var sectionObject = jObject.TryGetValue(section, out JToken sectionToken) ?
                JsonConvert.DeserializeObject<T>(sectionToken.ToString()) : (Value ?? new T());

            applyChanges(sectionObject);

            jObject[section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(file, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static void ConfigureWritable<T>(
            this IServiceCollection services,
            IConfigurationSection section,
            string file = "appsettings.json") where T : class, new()
        {
            //services.Configure<T>(section);
            services.AddTransient<IWritableOptions<T>>(provider =>
            {
                var options = provider.GetService<IOptionsMonitor<T>>();
                return new WritableOptions<T>(options, section.Key, file);
            });
        }
    }
}
