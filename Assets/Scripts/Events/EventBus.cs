using System;
using System.Collections.Generic;

namespace FourE.Events
{
    /// <summary>
    /// Bus di eventi statico per comunicazione decoupled tra sistemi.
    /// Gli eventi sono tipi <c>struct</c> fortemente tipizzati, mai stringhe.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new();

        /// <summary>
        /// Iscrive un handler agli eventi del tipo specificato.
        /// </summary>
        /// <typeparam name="T">Tipo evento struct.</typeparam>
        /// <param name="handler">Callback invocata alla pubblicazione.</param>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (Handlers.TryGetValue(type, out Delegate existing))
            {
                Handlers[type] = (Action<T>)existing + handler;
            }
            else
            {
                Handlers[type] = handler;
            }
        }

        /// <summary>
        /// Disiscrive un handler precedentemente registrato.
        /// </summary>
        /// <typeparam name="T">Tipo evento struct.</typeparam>
        /// <param name="handler">Callback da rimuovere.</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (!Handlers.TryGetValue(type, out Delegate existing))
            {
                return;
            }

            Delegate updated = (Action<T>)existing - handler;
            if (updated == null)
            {
                Handlers.Remove(type);
            }
            else
            {
                Handlers[type] = updated;
            }
        }

        /// <summary>
        /// Pubblica un evento a tutti gli handler iscritti al suo tipo.
        /// </summary>
        /// <typeparam name="T">Tipo evento struct.</typeparam>
        /// <param name="message">Istanza dell'evento da inoltrare.</param>
        public static void Publish<T>(T message) where T : struct
        {
            if (Handlers.TryGetValue(typeof(T), out Delegate existing) && existing is Action<T> callback)
            {
                callback.Invoke(message);
            }
        }

        /// <summary>
        /// Rimuove tutti gli handler. Da usare al teardown della partita per evitare leak.
        /// </summary>
        public static void Clear()
        {
            Handlers.Clear();
        }
    }
}
