using UnityEngine;
using System;
using System.Collections.Generic;

namespace ColorBlast.Core.Architecture
{
    /// <summary>
    /// Simple service locator for dependency injection
    /// </summary>
    public static class Services
    {
        private static Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// Register a service implementation
        /// </summary>
        public static void Register<T>(T implementation) where T : class
        {
            Type serviceType = typeof(T);
            
            if (services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"Service {serviceType.Name} is already registered. Overwriting.");
            }
            
            services[serviceType] = implementation;
            Debug.Log($"Service {serviceType.Name} registered successfully.");
        }

        /// <summary>
        /// Get a service implementation
        /// </summary>
        public static T Get<T>() where T : class
        {
            Type serviceType = typeof(T);
            
            if (services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }
            
            Debug.LogError($"Service {serviceType.Name} not found! Make sure it's registered before use.");
            return null;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Check if a service is registered (alias for IsRegistered)
        /// </summary>
        public static bool Has<T>() where T : class
        {
            return IsRegistered<T>();
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            Type serviceType = typeof(T);
            
            if (services.Remove(serviceType))
            {
                Debug.Log($"Service {serviceType.Name} unregistered successfully.");
            }
            else
            {
                Debug.LogWarning($"Service {serviceType.Name} was not registered.");
            }
        }

        /// <summary>
        /// Clear all services (useful for scene transitions)
        /// </summary>
        public static void Clear()
        {
            services.Clear();
            Debug.Log("All services cleared.");
        }

        /// <summary>
        /// Get all registered service types (for debugging)
        /// </summary>
        public static Type[] GetRegisteredServiceTypes()
        {
            Type[] types = new Type[services.Keys.Count];
            services.Keys.CopyTo(types, 0);
            return types;
        }
    }
}
