﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.ObjectBuilder.Common
{
    /// <summary>
    /// Implementation of IBuilder, serving as a facade that container specific implementations
    /// of IContainer should run behind.
    /// </summary>
    public class CommonObjectBuilder : IBuilder, IConfigureComponents
    {
        /// <summary>
        /// The container that will be used to create objects and configure components.
        /// </summary>
        public IContainer Container
        {
            get { return container; }
            set
            {
                container = value;
                if (sync != null)
                    sync.Container = value;
            }
        }

        private bool synchronized;

        /// <summary>
        /// Used for multi-threaded rich clients to build and dispatch
        /// in a synchronization domain.
        /// </summary>
        public bool Synchronized
        {
            get { return synchronized; }
            set
            {
                synchronized = value;

                if (synchronized)
                {
                    if (sync == null)
                        sync = new SynchronizedInvoker();

                    sync.Container = Container;
                }
            }
        }

        #region IConfigureComponents Members

        IComponentConfig IConfigureComponents.ConfigureComponent(Type concreteComponent, ComponentCallModelEnum callModel)
        {
            Container.Configure(concreteComponent, callModel);

            return new ComponentConfig(concreteComponent, Container);
        }

        IComponentConfig<T> IConfigureComponents.ConfigureComponent<T>(ComponentCallModelEnum callModel)
        {
            Container.Configure(typeof(T), callModel);

            return new ComponentConfig<T>(Container);
        }

        IConfigureComponents IConfigureComponents.ConfigureProperty<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);
            Container.ConfigureProperty(typeof(T), prop.Name, value);

            return this;
        }

        IConfigureComponents IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            Container.RegisterSingleton(lookupType, instance);
            return this;
        }

        IConfigureComponents IConfigureComponents.RegisterSingleton<T>(object instance)
        {
            Container.RegisterSingleton(typeof(T), instance);
            return this;
        }

        bool IConfigureComponents.HasComponent<T>()
        {
            return Container.HasComponent(typeof(T));
        }

        bool IConfigureComponents.HasComponent(Type componentType)
        {
            return Container.HasComponent(componentType);
        }

        #endregion

        #region IBuilder Members

        IBuilder IBuilder.CreateChildBuilder()
        {
            return new CommonObjectBuilder
            {
                Container = Container.BuildChildContainer()
            };
        }

        void IDisposable.Dispose()
        {
            Container.Dispose();
        }

        T IBuilder.Build<T>()
        {
            return (T)Container.Build(typeof(T));
        }

        object IBuilder.Build(Type typeToBuild)
        {
            return Container.Build(typeToBuild);
        }

        IEnumerable<object> IBuilder.BuildAll(Type typeToBuild)
        {
            return Container.BuildAll(typeToBuild);
        }

        IEnumerable<T> IBuilder.BuildAll<T>()
        {
            foreach (T element in Container.BuildAll(typeof(T)))
                yield return element;
        }

        void IBuilder.BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            if (sync != null)
                sync.BuildAndDispatch(typeToBuild, action);
            else
            {
                var o = Container.Build(typeToBuild);
                action(o);

                Container.ReleaseInstance(o);
            }
        }

        void IBuilder.ReleaseInstance(object instance)
        {
            Container.ReleaseInstance(instance);
        }

        #endregion

        private static SynchronizedInvoker sync;
        private IContainer container;
    }
}
