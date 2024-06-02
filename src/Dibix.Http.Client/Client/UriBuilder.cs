using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Dibix.Http.Client
{
    public sealed class UriBuilder
    {
        private readonly string _url;
        private readonly UriKind _kind;
        private readonly IDictionary<string, ICollection<string>> _params;

        private UriBuilder(string url, UriKind kind)
        {
            this._url = url;
            this._kind = kind;
            this._params = new Dictionary<string, ICollection<string>>();
        }

        public static UriBuilder Create(string url, UriKind kind = UriKind.Absolute) => new UriBuilder(url, kind);

        public UriBuilder AddQueryParam<T>(string name, T value) where T : struct => this.AddQueryParam(name, ToString(value));
        public UriBuilder AddQueryParam<T>(string name, T value, T defaultValue) where T : struct => !Equals(value, defaultValue) ? AddQueryParam(name, ToString(value)) : this;
        public UriBuilder AddQueryParam<T>(string name, T? value) where T : struct => value != null ? AddQueryParam(name, ToString(value)) : this;
        public UriBuilder AddQueryParam<T>(string name, T? value, T? defaultValue) where T : struct => !Equals(value, defaultValue) ? AddQueryParam(name, ToString(value)) : this;
        public UriBuilder AddQueryParam(string name, string value)
        {
            if (!this._params.TryGetValue(name, out ICollection<string> values))
            {
                values = new Collection<string>();
                this._params.Add(name, values);
            }
            values.Add(value);
            return this;
        }
        public UriBuilder AddQueryParam(string name, params string[] values)
        {
            foreach (string value in values) 
                this.AddQueryParam(name, value);

            return this;
        }
        public UriBuilder AddQueryParam<T>(string name, IEnumerable<T> values)
        {
            foreach (T value in values) 
                this.AddQueryParam(name, ToString(value));

            return this;
        }

        public Uri Build()
        {
            StringBuilder sb = new StringBuilder(this._url);

            if (this._params.Any())
            {
                sb.Append('?');

                bool firstParam = true;
                foreach (KeyValuePair<string, ICollection<string>> param in this._params)
                {
                    foreach (string value in param.Value)
                    {
                        if (!firstParam)
                            sb.Append('&');
                        else
                            firstParam = false;

                        sb.Append(param.Key);

                        bool isArrayParam = param.Value.Count > 1;
                        if (isArrayParam)
                            sb.Append("[]");

                        sb.Append('=')
                          .Append(value);
                    }
                }
            }

            string uri = sb.ToString();
            return new Uri(uri, this._kind);
        }

        private static string ToString<T>(T value)
        {
            Type type = typeof(T);
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                type = underlyingType;

            if (type == typeof(bool))
                return value.ToString()?.ToLowerInvariant();

            return value.ToString();
        }
    }
}