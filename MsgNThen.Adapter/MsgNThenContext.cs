using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace MsgNThen.Adapter
{
    public class MsgNThenContext : HttpContext
    {
        private static readonly Func<IFeatureCollection, IItemsFeature> _newItemsFeature = (Func<IFeatureCollection, IItemsFeature>)(f => (IItemsFeature)new ItemsFeature());
        private static readonly Func<IFeatureCollection, IServiceProvidersFeature> _newServiceProvidersFeature = (Func<IFeatureCollection, IServiceProvidersFeature>)(f => (IServiceProvidersFeature)new ServiceProvidersFeature());
        private static readonly Func<IFeatureCollection, IHttpRequestLifetimeFeature> _newHttpRequestLifetimeFeature = (Func<IFeatureCollection, IHttpRequestLifetimeFeature>)(f => (IHttpRequestLifetimeFeature)new HttpRequestLifetimeFeature());
        private static readonly Func<IFeatureCollection, ISessionFeature> _nullSessionFeature = (Func<IFeatureCollection, ISessionFeature>)(f => (ISessionFeature)null);
        private static readonly Func<IFeatureCollection, ISessionFeature> _newSessionFeature = (Func<IFeatureCollection, ISessionFeature>)(f => (ISessionFeature)new DefaultSessionFeature());
        private static readonly Func<IFeatureCollection, IHttpAuthenticationFeature> _newHttpAuthenticationFeature = (Func<IFeatureCollection, IHttpAuthenticationFeature>)(f => (IHttpAuthenticationFeature)new HttpAuthenticationFeature());
        private static readonly Func<IFeatureCollection, IHttpRequestIdentifierFeature> _newHttpRequestIdentifierFeature = (Func<IFeatureCollection, IHttpRequestIdentifierFeature>)(f => (IHttpRequestIdentifierFeature)new HttpRequestIdentifierFeature());

        //private static readonly Func<IFeatureCollection, IItemsFeature> _newItemsFeature = (Func<IFeatureCollection, IItemsFeature>)(f => (IItemsFeature)new Microsoft.AspNetCore.Http.Features.ItemsFeature());
        //private static readonly Func<IFeatureCollection, IServiceProvidersFeature> _newServiceProvidersFeature = (Func<IFeatureCollection, IServiceProvidersFeature>)(f => (IServiceProvidersFeature)new Microsoft.AspNetCore.Http.Features.ServiceProvidersFeature());
        //private static readonly Func<IFeatureCollection, IHttpRequestLifetimeFeature> _newHttpRequestLifetimeFeature = (Func<IFeatureCollection, IHttpRequestLifetimeFeature>)(f => (IHttpRequestLifetimeFeature)new HttpRequestLifetimeFeature());
        //private static readonly Func<IFeatureCollection, ISessionFeature> _nullSessionFeature = (Func<IFeatureCollection, ISessionFeature>)(f => (ISessionFeature)null);


        private FeatureReferences<MsgNThenContext.FeatureInterfaces> _features;
        private HttpRequest _request;
        private HttpResponse _response;
        private AuthenticationManager _authenticationManager;
        private ConnectionInfo _connection;
        private WebSocketManager _websockets;

        public MsgNThenContext()
            : this((IFeatureCollection)new FeatureCollection())
        {
            this.Features.Set<IHttpRequestFeature>((IHttpRequestFeature)new HttpRequestFeature());
            this.Features.Set<IHttpResponseFeature>((IHttpResponseFeature)new HttpResponseFeature());
        }

        public MsgNThenContext(IFeatureCollection features)
        {
            this.Initialize(features);
        }

        public virtual void Initialize(IFeatureCollection features)
        {
            this._features = new FeatureReferences<MsgNThenContext.FeatureInterfaces>(features);
            this._request = this.InitializeHttpRequest();
            this._response = this.InitializeHttpResponse();
        }

        public virtual void Uninitialize()
        {
            this._features = new FeatureReferences<MsgNThenContext.FeatureInterfaces>();
            if (this._request != null)
            {
                this.UninitializeHttpRequest(this._request);
                this._request = (HttpRequest)null;
            }
            if (this._response != null)
            {
                this.UninitializeHttpResponse(this._response);
                this._response = (HttpResponse)null;
            }
            if (this._authenticationManager != null)
            {
                this.UninitializeAuthenticationManager(this._authenticationManager);
                this._authenticationManager = (AuthenticationManager)null;
            }
            if (this._connection != null)
            {
                this.UninitializeConnectionInfo(this._connection);
                this._connection = (ConnectionInfo)null;
            }
            if (this._websockets == null)
                return;
            this.UninitializeWebSocketManager(this._websockets);
            this._websockets = (WebSocketManager)null;
        }

        private IItemsFeature ItemsFeature
        {
            get
            {
                return this._features.Fetch<IItemsFeature>(ref this._features.Cache.Items, MsgNThenContext._newItemsFeature);
            }
        }

        private IServiceProvidersFeature ServiceProvidersFeature
        {
            get
            {
                return this._features.Fetch<IServiceProvidersFeature>(ref this._features.Cache.ServiceProviders, MsgNThenContext._newServiceProvidersFeature);
            }
        }
        private IHttpRequestIdentifierFeature RequestIdentifierFeature
        {
            get
            {
                return this._features.Fetch<IHttpRequestIdentifierFeature>(ref this._features.Cache.RequestIdentifier, MsgNThenContext._newHttpRequestIdentifierFeature);
            }
        }

        private IHttpAuthenticationFeature HttpAuthenticationFeature
        {
            get
            {
                return this._features.Fetch<IHttpAuthenticationFeature>(ref this._features.Cache.Authentication, MsgNThenContext._newHttpAuthenticationFeature);
            }
        }

        private IHttpRequestLifetimeFeature LifetimeFeature
        {
            get
            {
                return this._features.Fetch<IHttpRequestLifetimeFeature>(ref this._features.Cache.Lifetime, MsgNThenContext._newHttpRequestLifetimeFeature);
            }
        }

        private ISessionFeature SessionFeature
        {
            get
            {
                return this._features.Fetch<ISessionFeature>(ref this._features.Cache.Session, MsgNThenContext._newSessionFeature);
            }
        }

        private ISessionFeature SessionFeatureOrNull
        {
            get
            {
                return this._features.Fetch<ISessionFeature>(ref this._features.Cache.Session, MsgNThenContext._nullSessionFeature);
            }
        }

        public override IFeatureCollection Features
        {
            get
            {
                return this._features.Collection;
            }
        }

        public override HttpRequest Request
        {
            get
            {
                return this._request;
            }
        }

        public override HttpResponse Response
        {
            get
            {
                return this._response;
            }
        }

        public override ConnectionInfo Connection
        {
            get
            {
                return this._connection ?? (this._connection = this.InitializeConnectionInfo());
            }
        }

        /// <summary>
        /// This is obsolete and will be removed in a future version.
        /// The recommended alternative is to use Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.
        /// See https://go.microsoft.com/fwlink/?linkid=845470.
        /// </summary>
        [Obsolete("This is obsolete and will be removed in a future version. The recommended alternative is to use Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions. See https://go.microsoft.com/fwlink/?linkid=845470.")]
        public override AuthenticationManager Authentication
        {
            get
            {
                return this._authenticationManager ?? (this._authenticationManager = this.InitializeAuthenticationManager());
            }
        }

        public override WebSocketManager WebSockets
        {
            get
            {
                return this._websockets ?? (this._websockets = this.InitializeWebSocketManager());
            }
        }

        public override ClaimsPrincipal User
        {
            get
            {
                ClaimsPrincipal claimsPrincipal = this.HttpAuthenticationFeature.User;
                if (claimsPrincipal == null)
                {
                    claimsPrincipal = new ClaimsPrincipal((IIdentity)new ClaimsIdentity());
                    this.HttpAuthenticationFeature.User = claimsPrincipal;
                }
                return claimsPrincipal;
            }
            set
            {
                this.HttpAuthenticationFeature.User = value;
            }
        }

        public override IDictionary<object, object> Items
        {
            get
            {
                return this.ItemsFeature.Items;
            }
            set
            {
                this.ItemsFeature.Items = value;
            }
        }

        public override IServiceProvider RequestServices
        {
            get
            {
                return this.ServiceProvidersFeature.RequestServices;
            }
            set
            {
                this.ServiceProvidersFeature.RequestServices = value;
            }
        }

        public override CancellationToken RequestAborted
        {
            get
            {
                return this.LifetimeFeature.RequestAborted;
            }
            set
            {
                this.LifetimeFeature.RequestAborted = value;
            }
        }

        public override string TraceIdentifier
        {
            get
            {
                return this.RequestIdentifierFeature.TraceIdentifier;
            }
            set
            {
                this.RequestIdentifierFeature.TraceIdentifier = value;
            }
        }

        public override ISession Session
        {
            get
            {
                ISessionFeature sessionFeatureOrNull = this.SessionFeatureOrNull;
                if (sessionFeatureOrNull == null)
                    throw new InvalidOperationException("Session has not been configured for this application or request.");
                return sessionFeatureOrNull.Session;
            }
            set
            {
                this.SessionFeature.Session = value;
            }
        }

        public override void Abort()
        {
            this.LifetimeFeature.Abort();
        }

        protected virtual HttpRequest InitializeHttpRequest()
        {
            return (HttpRequest)new DefaultHttpRequest((HttpContext)this);
        }

        protected virtual void UninitializeHttpRequest(HttpRequest instance)
        {
        }

        protected virtual HttpResponse InitializeHttpResponse()
        {
            return (HttpResponse)new DefaultHttpResponse((HttpContext)this);
        }

        protected virtual void UninitializeHttpResponse(HttpResponse instance)
        {
        }

        protected virtual ConnectionInfo InitializeConnectionInfo()
        {
            return null;
        }

        protected virtual void UninitializeConnectionInfo(ConnectionInfo instance)
        {
        }

        [Obsolete("This is obsolete and will be removed in a future version. See https://go.microsoft.com/fwlink/?linkid=845470.")]
        protected virtual AuthenticationManager InitializeAuthenticationManager()
        {
            return null;
        }

        [Obsolete("This is obsolete and will be removed in a future version. See https://go.microsoft.com/fwlink/?linkid=845470.")]
        protected virtual void UninitializeAuthenticationManager(AuthenticationManager instance)
        {
        }

        protected virtual WebSocketManager InitializeWebSocketManager()
        {
            return null;
        }

        protected virtual void UninitializeWebSocketManager(WebSocketManager instance)
        {
        }

        private struct FeatureInterfaces
        {
            public IItemsFeature Items;
            public IServiceProvidersFeature ServiceProviders;
            public IHttpAuthenticationFeature Authentication;
            public IHttpRequestLifetimeFeature Lifetime;
            public ISessionFeature Session;
            public IHttpRequestIdentifierFeature RequestIdentifier;
        }
    }
}