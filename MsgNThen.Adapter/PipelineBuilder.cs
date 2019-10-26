using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class PipelineBuilder : IApplicationBuilder
    {
        private readonly IList<Func<RequestDelegate, RequestDelegate>> _components = (IList<Func<RequestDelegate, RequestDelegate>>)new List<Func<RequestDelegate, RequestDelegate>>();

        public PipelineBuilder(IServiceProvider serviceProvider)
        {
            this.Properties = (IDictionary<string, object>)new Dictionary<string, object>((IEqualityComparer<string>)StringComparer.Ordinal);
            this.ApplicationServices = serviceProvider;
        }

        public PipelineBuilder(IServiceProvider serviceProvider, object server)
          : this(serviceProvider)
        {
            this.SetProperty<object>(Constants.BuilderProperties.ServerFeatures, server);
        }

        private PipelineBuilder(PipelineBuilder builder)
        {
            this.Properties = (IDictionary<string, object>)new CopyOnWriteDictionary<string, object>(builder.Properties, (IEqualityComparer<string>)StringComparer.Ordinal);
        }

        public IServiceProvider ApplicationServices
        {
            get
            {
                return this.GetProperty<IServiceProvider>(Constants.BuilderProperties.ApplicationServices);
            }
            set
            {
                this.SetProperty<IServiceProvider>(Constants.BuilderProperties.ApplicationServices, value);
            }
        }

        public IFeatureCollection ServerFeatures
        {
            get
            {
                return this.GetProperty<IFeatureCollection>(Constants.BuilderProperties.ServerFeatures);
            }
        }

        public IDictionary<string, object> Properties { get; }

        private T GetProperty<T>(string key)
        {
            object obj;
            if (!this.Properties.TryGetValue(key, out obj))
                return default(T);
            return (T)obj;
        }

        private void SetProperty<T>(string key, T value)
        {
            this.Properties[key] = (object)value;
        }

        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            this._components.Add(middleware);
            return (IApplicationBuilder)this;
        }

        public IApplicationBuilder New()
        {
            return (IApplicationBuilder)new PipelineBuilder(this);
        }

        public RequestDelegate Build()
        {
            RequestDelegate requestDelegate = (RequestDelegate)(context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });
            foreach (Func<RequestDelegate, RequestDelegate> func in this._components.Reverse<Func<RequestDelegate, RequestDelegate>>())
                requestDelegate = func(requestDelegate);
            return requestDelegate;
        }
    }
}