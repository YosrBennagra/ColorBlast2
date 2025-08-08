using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorBlast.Core.Architecture
{
    /// <summary>
    /// Simple dependency injection container for managing services
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator instance;
        private Dictionary<Type, object> services = new Dictionary<Type, object>();

        public static ServiceLocator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<ServiceLocator>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ServiceLocator");
                        instance = go.AddComponent<ServiceLocator>();
                        
                        // Only use DontDestroyOnLoad in play mode
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Register a service with the container
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            Type serviceType = typeof(T);
            
            if (services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"Service {serviceType.Name} is already registered. Replacing...");
            }
            
            services[serviceType] = service;
            Debug.Log($"Registered service: {serviceType.Name}");
        }

        /// <summary>
        /// Get a service from the container
        /// </summary>
        public T GetService<T>() where T : class
        {
            Type serviceType = typeof(T);
            
            if (services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }
            
            Debug.LogError($"Service {serviceType.Name} not found!");
            return null;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool HasService<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void UnregisterService<T>() where T : class
        {
            Type serviceType = typeof(T);
            
            if (services.Remove(serviceType))
            {
                Debug.Log($"Unregistered service: {serviceType.Name}");
            }
        }

        /// <summary>
        /// Clear all services (useful for scene transitions)
        /// </summary>
        public void ClearServices()
        {
            services.Clear();
            Debug.Log("All services cleared");
        }

        /// <summary>
        /// Get all registered service types (for debugging)
        /// </summary>
        public List<Type> GetRegisteredServiceTypes()
        {
            return new List<Type>(services.Keys);
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                
                // Only use DontDestroyOnLoad in play mode
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
