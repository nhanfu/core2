using Bridge.Html5;
using Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Core.Clients
{
    public class LocalStorage
    {
        private static bool _hasRegisteredCrossTab;
        public static Dictionary<string, List<Action<string, object, object>>> StorageEventMap = new Dictionary<string, List<Action<string, object, object>>>();
        public static Storage Instance => Window.LocalStorage;
        public static void Subscribe(string key, Action<string, object, object> action, Type type, bool croosTab = true)
        {
            var localActions = StorageEventMap.GetValueOrDefault(key);
            localActions.Add(action);
            if (!croosTab || _hasRegisteredCrossTab)
            {
                return;
            }
            _hasRegisteredCrossTab = true;
            Window.AddEventListener(EventType.Storage, (e) =>
            {
                var eventKey = e["key"]?.ToString();
                if (eventKey != key)
                {
                    return;
                }
                var actions = StorageEventMap.GetValueOrDefault(key);
                if (actions.Nothing())
                {
                    return;
                }
                var newVal = e["newValue"] as string;
                var oldVal = e["oldValue"] as string;
                actions?.ForEach(x => x.Invoke(key, JsonConvert.DeserializeObject(newVal, type), JsonConvert.DeserializeObject(oldVal, type)));
            });
        }

        public static void Ubsubscribe(string key, Action<string, object, object> action)
        {
            var actions = StorageEventMap.GetValueOrDefault(key);
            actions.Remove(action);
        }

        public static void SetItem<T>(string key, T val)
        {
            if (val == null)
            {
                Instance.SetItem(key, null);
            }
            else
            {
                Instance.SetItem(key, typeof(T) == typeof(string) ? val.ToString() : JsonConvert.SerializeObject(val));
            }
            var localActions = StorageEventMap.GetValueOrDefault(key);
            localActions?.ForEach(x => x.Invoke(key, val, GetItem<T>(key)));
        }

        public static T GetItem<T>(string key)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Instance.GetItem(key);
            }
            var data = Instance.GetItem(key) as string;
            return data == null ? default(T) : JsonConvert.DeserializeObject<T>(data);
        }

        public static void RemoveItem(string key)
        {
            Instance.RemoveItem(key);
        }
    }
}
