// Minimal compile-only stand-ins for APIs supplied by Rust, uMod, and Newtonsoft.Json.
// These types do not implement runtime behaviour and must never be shipped to a server.
using System;
using System.Collections.Generic;

namespace Newtonsoft.Json
{
    public sealed class JsonPropertyAttribute : Attribute
    {
        public JsonPropertyAttribute(string value) { }
    }

    public sealed class JsonIgnoreAttribute : Attribute { }

    public static class JsonConvert
    {
        public static T DeserializeObject<T>(string value) => default(T);
        public static string SerializeObject(object value) => string.Empty;
    }
}

namespace Oxide.Core.Libraries
{
    public enum RequestMethod
    {
        GET,
        POST
    }
}

namespace Oxide.Core
{
    public class Timer
    {
        public void Destroy() { }
    }

    public static class Interface
    {
        public static OxideRoot Oxide { get; } = new OxideRoot();
    }

    public class OxideRoot
    {
        public DataFiles DataFileSystem { get; } = new DataFiles();
    }

    public class DataFiles
    {
        public T ReadObject<T>(string name) => default(T);
        public void WriteObject<T>(string name, T value) { }
    }
}

namespace Oxide.Plugins
{
    using Oxide.Core;

    public sealed class InfoAttribute : Attribute
    {
        public InfoAttribute(string name, string author, string version) { }
    }

    public sealed class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description) { }
    }

    public sealed class ConsoleCommandAttribute : Attribute
    {
        public ConsoleCommandAttribute(string command) { }
    }

    public sealed class ChatCommandAttribute : Attribute
    {
        public ChatCommandAttribute(string command) { }
    }

    public class RustPlugin
    {
        protected Permission permission = new Permission();
        protected Language lang = new Language();
        protected TimerManager timer = new TimerManager();
        protected WebRequests webrequest = new WebRequests();
        protected DynamicConfigFile Config = new DynamicConfigFile();

        protected virtual void LoadDefaultConfig() { }
        protected virtual void LoadDefaultMessages() { }
        protected virtual void LoadConfig() { }
        protected virtual void SaveConfig() { }
        protected void Puts(string value) { }
        protected void PrintError(string value) { }
        protected void PrintWarning(string value) { }
    }

    public class Permission
    {
        public void RegisterPermission(string permission, object owner) { }
        public bool UserHasPermission(string userId, string permission) => false;
    }

    public class Language
    {
        public void RegisterMessages(
            Dictionary<string, string> messages,
            object owner,
            string language = null) { }

        public string GetMessage(string key, object owner, string userId) => key;
    }

    public class TimerManager
    {
        public Timer Every(float seconds, Action callback) => new Timer();
        public Timer Once(float seconds, Action callback) => new Timer();
    }

    public class WebRequests
    {
        public void Enqueue(
            string url,
            string body,
            Action<int, string> callback,
            object owner) { }

        public void Enqueue(
            string url,
            string body,
            Action<int, string> callback,
            object owner,
            Oxide.Core.Libraries.RequestMethod method,
            Dictionary<string, string> headers) { }
    }

    public class DynamicConfigFile
    {
        public T ReadObject<T>() => default(T);
        public void WriteObject<T>(T value) { }
    }
}

public class BasePlayer
{
    public static List<BasePlayer> activePlayerList = new List<BasePlayer>();

    public string UserIDString { get; set; }
    public string displayName { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsConnected { get; set; }

    public static BasePlayer FindByID(ulong id) => null;
    public void Kick(string reason) { }
    public void ChatMessage(string message) { }
    public void SendConsoleCommand(string command, params object[] args) { }
}

public static class ConsoleSystem
{
    public class Arg
    {
        public Connection Connection { get; set; }
        public string GetString(int index) => string.Empty;
        public void ReplyWith(string message) { }
    }

    public class Connection
    {
        public object player { get; set; }
    }
}
