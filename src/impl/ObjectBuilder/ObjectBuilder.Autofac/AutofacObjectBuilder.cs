using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using NServiceBus.ObjectBuilder.Autofac.Internal;

namespace NServiceBus.ObjectBuilder.Autofac
{
    ///<summary>
    /// Autofac implementation of IContainer.
    ///</summary>
    internal class AutofacObjectBuilder : Common.IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private readonly ILifetimeScope container;

        private bool disposed;

        ///<summary>
        /// Instantiates the class utilizing the given container.
        ///</summary>
        ///<param name="container"></param>
        public AutofacObjectBuilder(ILifetimeScope container)
        {
            this.container = container;
        }

        ///<summary>
        /// Instantites the class with an empty Autofac container.
        ///</summary>
        public AutofacObjectBuilder() : this(new ContainerBuilder().Build())
        {
        }

        /// <summary>
        /// Disposes the container and all resources instantiated by the container.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            disposed = true;
            container.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <returns></returns>
        public Common.IContainer BuildChildContainer()
        {
            return new AutofacObjectBuilder(container.BeginLifetimeScope());
        }
        
        ///<summary>
        /// Build an instance of a given type using Autofac.
        ///</summary>
        ///<param name="typeToBuild"></param>
        ///<returns></returns>
        public object Build(Type typeToBuild)
        {
            return container.Resolve(typeToBuild);
        }

        ///<summary>
        /// Build all instances of a given type using Autofac.
        ///</summary>
        ///<param name="typeToBuild"></param>
        ///<returns></returns>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return container.ResolveAll(typeToBuild);
        }

        /// <summary>
        /// Tells Autofac to release the given instance.
        /// </summary>
        /// <param name="instance"></param>
        public void ReleaseInstance(object instance)
        {
            //no-op
        }

        void Common.IContainer.Configure(Type component, ComponentCallModelEnum callModel)
        {
            IComponentRegistration registration = this.GetComponentRegistration(component);

            if (registration == null)
            {
                var builder = new ContainerBuilder();
                Type[] services = component.GetAllServices().ToArray();
                var registrationBuilder = builder.RegisterType(component).As(services).PropertiesAutowired();
                
                SetLifetimeScope(callModel, registrationBuilder);
                
                builder.Update(this.container.ComponentRegistry);
            }
        }

        ///<summary>
        /// Configure the value of a named component property.
        ///</summary>
        ///<param name="component"></param>
        ///<param name="property"></param>
        ///<param name="value"></param>
        public void ConfigureProperty(Type component, string property, object value)
        {
            var registration = GetComponentRegistration(component);

            if (registration == null)
            {
                throw new InvalidOperationException(
                    "Cannot configure properties for a type that hasn't been configured yet: " + component.FullName);
            }

            registration.Activating += (sender, e) => e.Instance.SetPropertyValue(property, value);
        }

        ///<summary>
        /// Register a singleton instance of a dependency within Autofac.
        ///</summary>
        ///<param name="lookupType"></param>
        ///<param name="instance"></param>
        public void RegisterSingleton(Type lookupType, object instance)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance).As(lookupType).ExternallyOwned().PropertiesAutowired();
            builder.Update(this.container.ComponentRegistry);
        }

        public bool HasComponent(Type componentType)
        {
            return container.IsRegistered(componentType);
        }

        private static void SetLifetimeScope(ComponentCallModelEnum callModel, IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registrationBuilder)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singlecall:
                    registrationBuilder.InstancePerDependency();
                    break;
                case ComponentCallModelEnum.Singleton:
                    registrationBuilder.SingleInstance();
                    break;
                default:
                    registrationBuilder.InstancePerLifetimeScope();
                    break;
            }
        }

        private IComponentRegistration GetComponentRegistration(Type concreteComponent)
        {
            return this.container.ComponentRegistry.Registrations.Where(x => x.Activator.LimitType == concreteComponent).FirstOrDefault();
        }
    }
}


